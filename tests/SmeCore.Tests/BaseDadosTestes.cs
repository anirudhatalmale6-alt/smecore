using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using Xunit;

namespace SmeCore.Tests;

/// <summary>
/// Todas as classes de teste de base de dados partilham a mesma base: têm de correr uma de
/// cada vez, senão apagam a base umas das outras a meio. Esta coleção garante essa serialização.
/// </summary>
[CollectionDefinition("BaseDados", DisableParallelization = true)]
public class ColecaoBaseDados { }

/// <summary>
/// Testes que precisam mesmo de PostgreSQL — ILIKE, jsonb, índices filtrados e as sequências
/// não existem em nenhum motor em memória, e testá-los com um substituto daria confiança falsa.
///
/// Correm quando a variável TEST_POSTGRES_URL estiver definida, por exemplo:
///   TEST_POSTGRES_URL="Host=localhost;Port=5432;Database=smecore_testes;Username=postgres;Password=postgres" dotnet test
///
/// Sem essa variável são marcados como IGNORADOS (nunca como aprovados).
/// </summary>
[Collection("BaseDados")]
public abstract class TesteComBaseDados : IAsyncLifetime
{
    protected AppDbContext Db { get; private set; } = null!;
    protected IRelogio Relogio { get; } = new RelogioSistema();

    private string? _ligacao;

    public async Task InitializeAsync()
    {
        _ligacao = Environment.GetEnvironmentVariable("TEST_POSTGRES_URL");
        Skip.If(string.IsNullOrWhiteSpace(_ligacao),
            "Defina TEST_POSTGRES_URL para correr os testes de base de dados.");

        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_ligacao)
            .AddInterceptors(new AuditoriaInterceptor(new UtilizadorDeTeste(), Relogio))
            .Options;

        Db = new AppDbContext(opcoes);

        await Db.Database.EnsureDeletedAsync();
        await Db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Db is not null) await Db.DisposeAsync();
    }

    protected sealed class UtilizadorDeTeste : IUtilizadorAtual
    {
        public Guid? Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? Nome => "Teste";
        public string? Origem => "127.0.0.1";
    }

    protected static Cliente NovoCliente(string nome = "Ana Ribeiro", string? nif = "123456789") => new()
    {
        Nome = nome,
        Nif = nif,
        Tipo = TipoCliente.Particular,
        Localidade = "Braga",
        Ativo = true
    };
}

public class AuditoriaIntegracaoTestes : TesteComBaseDados
{
    [SkippableFact]
    public async Task Criar_um_cliente_escreve_uma_linha_de_historico()
    {
        var cliente = NovoCliente();
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        var registos = await Db.RegistosAuditoria.Where(r => r.EntidadeId == cliente.Id.ToString()).ToListAsync();

        registos.Should().HaveCount(1);
        registos[0].Acao.Should().Be(AcaoAuditoria.Criacao);
        registos[0].Entidade.Should().Be(nameof(Cliente));
        registos[0].UtilizadorNome.Should().Be("Teste");
        registos[0].EntidadeDescricao.Should().Be("Ana Ribeiro");
        registos[0].AlteracoesJson.Should().Contain("Nome");
    }

    [SkippableFact]
    public async Task Historico_de_criacao_nao_inclui_colunas_geradas_pela_base_de_dados()
    {
        var cliente = NovoCliente();
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        var registo = await Db.RegistosAuditoria.SingleAsync(r => r.EntidadeId == cliente.Id.ToString());

        // "Número: 0" seria mentira: o número é atribuído pela sequência ao gravar.
        registo.AlteracoesJson.Should().NotContain("\"Numero\"");
    }

    [SkippableFact]
    public async Task Alterar_um_campo_guarda_o_valor_antigo_e_o_novo()
    {
        var cliente = NovoCliente();
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        cliente.Localidade = "Guimarães";
        await Db.SaveChangesAsync();

        var alteracao = await Db.RegistosAuditoria
            .Where(r => r.EntidadeId == cliente.Id.ToString() && r.Acao == AcaoAuditoria.Alteracao)
            .SingleAsync();

        // Sem escapes \uXXXX: é o que permite pesquisar "Guimarães" no histórico.
        alteracao.AlteracoesJson.Should().Contain("Braga").And.Contain("Guimarães");
    }

    [SkippableFact]
    public async Task Gravar_sem_alteracoes_nao_escreve_historico()
    {
        var cliente = NovoCliente();
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        cliente.Localidade = "Braga"; // mesmo valor
        await Db.SaveChangesAsync();

        var total = await Db.RegistosAuditoria.CountAsync(r => r.EntidadeId == cliente.Id.ToString());
        total.Should().Be(1);
    }

    [SkippableFact]
    public async Task Eliminacao_logica_e_registada_como_eliminacao_e_o_registo_permanece()
    {
        var cliente = NovoCliente();
        Db.Clientes.Add(cliente);
        await Db.SaveChangesAsync();

        cliente.Eliminado = true;
        await Db.SaveChangesAsync();

        var registo = await Db.RegistosAuditoria
            .Where(r => r.EntidadeId == cliente.Id.ToString())
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        registo.Acao.Should().Be(AcaoAuditoria.Eliminacao);

        // O registo continua na tabela — é isso que mantém o histórico coerente.
        (await Db.Clientes.CountAsync(c => c.Id == cliente.Id)).Should().Be(1);
        cliente.EliminadoEm.Should().NotBeNull();
        cliente.EliminadoPorNome.Should().Be("Teste");
    }

    [SkippableFact]
    public async Task Palavra_passe_nunca_entra_no_historico()
    {
        var utilizador = new Utilizador
        {
            UserName = "teste@exemplo.pt",
            Email = "teste@exemplo.pt",
            NomeCompleto = "Conta de Teste",
            PasswordHash = "SEGREDO-QUE-NAO-PODE-APARECER"
        };

        Db.Users.Add(utilizador);
        await Db.SaveChangesAsync();

        var registo = await Db.RegistosAuditoria.SingleAsync(r => r.EntidadeId == utilizador.Id.ToString());

        registo.AlteracoesJson.Should().NotContain("SEGREDO-QUE-NAO-PODE-APARECER");
        registo.AlteracoesJson.Should().NotContain("PasswordHash");
    }
}

public class ClientesServicoTestes : TesteComBaseDados
{
    private ClientesServico Servico => new(Db);

    [SkippableFact]
    public async Task Nif_repetido_e_recusado()
    {
        (await Servico.CriarAsync(NovoCliente("Primeiro", "123456789"))).Sucesso.Should().BeTrue();

        var segundo = await Servico.CriarAsync(NovoCliente("Segundo", "123 456 789"));

        segundo.Sucesso.Should().BeFalse();
        segundo.Erros[0].Campo.Should().Be(nameof(Cliente.Nif));
    }

    [SkippableFact]
    public async Task Nif_de_um_cliente_eliminado_pode_ser_reutilizado()
    {
        var primeiro = await Servico.CriarAsync(NovoCliente("Primeiro", "123456789"));
        await Servico.EliminarAsync(primeiro.Valor!.Id);

        var segundo = await Servico.CriarAsync(NovoCliente("Segundo", "123456789"));

        segundo.Sucesso.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Pesquisa_ignora_maiusculas()
    {
        await Servico.CriarAsync(NovoCliente("Construções Serra Norte", "501442600"));

        // ILIKE resolve maiúsculas/minúsculas. Não resolve acentos: procurar "construcoes"
        // (sem cedilha nem til) não encontraria este cliente — precisaria da extensão unaccent.
        var pagina = await Servico.ObterPaginaAsync(new ClientesFiltro { Pesquisa = "construções" });

        pagina.TotalItens.Should().Be(1);
    }

    [SkippableFact]
    public async Task Pesquisa_com_percentagem_nao_devolve_tudo()
    {
        await Servico.CriarAsync(NovoCliente("Ana", "123456789"));
        await Servico.CriarAsync(NovoCliente("Bruno", "249123454"));

        // Sem escape, "%" seria interpretado como "qualquer coisa" e devolveria os dois.
        var pagina = await Servico.ObterPaginaAsync(new ClientesFiltro { Pesquisa = "%" });

        pagina.TotalItens.Should().Be(0);
    }

    [SkippableFact]
    public async Task Paginacao_nao_repete_nem_salta_registos_quando_o_campo_ordenado_empata()
    {
        // Vinte clientes com o mesmo nome: sem critério de desempate, a base de dados pode
        // devolver as páginas por ordens diferentes e alguns registos apareceriam duas vezes.
        for (var i = 0; i < 20; i++)
        {
            await Servico.CriarAsync(NovoCliente("Nome Repetido", nif: null));
        }

        var ids = new List<Guid>();
        for (var pagina = 1; pagina <= 4; pagina++)
        {
            var resultado = await Servico.ObterPaginaAsync(new ClientesFiltro
            {
                Ordenar = "nome",
                Pagina = pagina,
                TamanhoPagina = 5
            });
            ids.AddRange(resultado.Itens.Select(c => c.Id));
        }

        ids.Should().OnlyHaveUniqueItems();
        ids.Should().HaveCount(20);
    }

    [SkippableFact]
    public async Task Pagina_alem_do_fim_recua_para_a_ultima_com_resultados()
    {
        await Servico.CriarAsync(NovoCliente("Ana", "123456789"));

        var pagina = await Servico.ObterPaginaAsync(new ClientesFiltro { Pagina = 50, TamanhoPagina = 25 });

        pagina.Pagina.Should().Be(1);
        pagina.Itens.Should().HaveCount(1);
    }

    [SkippableFact]
    public async Task Campo_de_ordenacao_desconhecido_nao_rebenta_a_consulta()
    {
        await Servico.CriarAsync(NovoCliente("Ana", "123456789"));

        var pagina = await Servico.ObterPaginaAsync(new ClientesFiltro { Ordenar = "'; drop table clientes; --" });

        pagina.TotalItens.Should().Be(1);
        (await Db.Clientes.CountAsync()).Should().Be(1);
    }

    [SkippableFact]
    public async Task Cliente_com_veiculos_ativos_nao_pode_ser_eliminado()
    {
        var cliente = (await Servico.CriarAsync(NovoCliente())).Valor!;

        var veiculos = new VeiculosServico(Db, Relogio);
        await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id,
            Matricula = "AA-00-AA",
            Marca = "Renault",
            Modelo = "Clio"
        });

        var resultado = await Servico.EliminarAsync(cliente.Id);

        resultado.Sucesso.Should().BeFalse();
        resultado.Erros[0].Mensagem.Should().Contain("veículo");
    }
}

public class VeiculosServicoTestes : TesteComBaseDados
{
    [SkippableFact]
    public async Task Matricula_e_normalizada_e_a_repeticao_recusada()
    {
        var clientes = new ClientesServico(Db);
        var veiculos = new VeiculosServico(Db, Relogio);
        var cliente = (await clientes.CriarAsync(NovoCliente())).Valor!;

        var primeiro = await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id,
            Matricula = "aa00aa",
            Marca = "Renault",
            Modelo = "Clio"
        });

        primeiro.Sucesso.Should().BeTrue();
        primeiro.Valor!.Matricula.Should().Be("AA-00-AA");

        var segundo = await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id,
            Matricula = "AA 00 AA",
            Marca = "Opel",
            Modelo = "Corsa"
        });

        segundo.Sucesso.Should().BeFalse();
        segundo.Erros[0].Campo.Should().Be(nameof(Veiculo.Matricula));
    }

    [SkippableFact]
    public async Task Filtro_de_inspecao_vencida_usa_a_data_do_relogio_e_nao_uma_data_fixa()
    {
        var clientes = new ClientesServico(Db);
        var veiculos = new VeiculosServico(Db, Relogio);
        var cliente = (await clientes.CriarAsync(NovoCliente())).Valor!;

        var hoje = Relogio.HojeLocal;

        await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id, Matricula = "AA-00-AA", Marca = "A", Modelo = "1",
            ProximaInspecao = hoje.AddDays(-1)
        });
        await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id, Matricula = "BB-00-BB", Marca = "B", Modelo = "2",
            ProximaInspecao = hoje.AddDays(10)
        });
        await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = cliente.Id, Matricula = "CC-00-CC", Marca = "C", Modelo = "3",
            ProximaInspecao = null
        });

        var vencidos = await veiculos.ObterPaginaAsync(new VeiculosFiltro { InspecaoVencida = true });
        vencidos.TotalItens.Should().Be(1);
        vencidos.Itens[0].Matricula.Should().Be("AA-00-AA");

        var proximos = await veiculos.ObterPaginaAsync(new VeiculosFiltro { InspecaoProximosDias = 30 });
        proximos.TotalItens.Should().Be(1);
        proximos.Itens[0].Matricula.Should().Be("BB-00-BB");

        var resumo = await veiculos.ObterResumoInspecoesAsync();
        resumo.Total.Should().Be(3);
        resumo.Vencidos.Should().Be(1);
        resumo.Proximos30.Should().Be(1);
    }

    [SkippableFact]
    public async Task Veiculo_sem_cliente_valido_e_recusado()
    {
        var veiculos = new VeiculosServico(Db, Relogio);

        var resultado = await veiculos.CriarAsync(new Veiculo
        {
            ClienteId = Guid.NewGuid(),
            Matricula = "AA-00-AA",
            Marca = "Renault",
            Modelo = "Clio"
        });

        resultado.Sucesso.Should().BeFalse();
        resultado.Erros[0].Campo.Should().Be(nameof(Veiculo.ClienteId));
    }

    [SkippableFact]
    public async Task Data_gravada_e_lida_em_utc()
    {
        var clientes = new ClientesServico(Db);
        var cliente = (await clientes.CriarAsync(NovoCliente())).Valor!;

        // O que ficou na base de dados tem de ser o instante UTC, independentemente do fuso
        // do servidor: é isso que permite mudar de servidor sem deslocar o histórico.
        var lido = await Db.Clientes.AsNoTracking().SingleAsync(c => c.Id == cliente.Id);

        lido.CriadoEm.Offset.Should().Be(TimeSpan.Zero);
        lido.CriadoEm.Should().BeCloseTo(Relogio.AgoraUtc, TimeSpan.FromMinutes(5));
    }
}
