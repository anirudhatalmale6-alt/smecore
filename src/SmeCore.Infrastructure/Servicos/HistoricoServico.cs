using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Consultas;
using SmeCore.Infrastructure.Dados;

namespace SmeCore.Infrastructure.Servicos;

public class HistoricoFiltro : ParametrosListagem
{
    public string? Entidade { get; set; }
    public string? EntidadeId { get; set; }
    public AcaoAuditoria? Acao { get; set; }
    public Guid? UtilizadorId { get; set; }
    public DateOnly? De { get; set; }
    public DateOnly? Ate { get; set; }

    public bool TemFiltrosAtivos =>
        !string.IsNullOrWhiteSpace(Pesquisa)
        || !string.IsNullOrWhiteSpace(Entidade)
        || Acao.HasValue
        || UtilizadorId.HasValue
        || De.HasValue
        || Ate.HasValue;
}

/// <summary>Linha do histórico já preparada para apresentação, com as alterações desserializadas.</summary>
public class HistoricoLinha
{
    public long Id { get; init; }
    public DateTimeOffset Instante { get; init; }
    public string Entidade { get; init; } = string.Empty;
    public string EntidadeId { get; init; } = string.Empty;
    public string? EntidadeDescricao { get; init; }
    public AcaoAuditoria Acao { get; init; }
    public string? UtilizadorNome { get; init; }
    public string? Origem { get; init; }
    public IReadOnlyList<AlteracaoCampo> Alteracoes { get; init; } = Array.Empty<AlteracaoCampo>();
}

public class HistoricoServico
{
    /// <summary>
    /// Nomes técnicos das entidades e dos campos traduzidos para português. O histórico é para
    /// ser lido por quem usa a aplicação, não por quem escreveu o modelo.
    /// </summary>
    private static readonly Dictionary<string, string> NomesEntidades = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cliente"] = "Cliente",
        ["ClienteContacto"] = "Contacto de cliente",
        ["Veiculo"] = "Veículo",
        ["Utilizador"] = "Utilizador",
        ["Perfil"] = "Perfil de acesso"
    };

    private static readonly Dictionary<string, string> NomesCampos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nome"] = "Nome",
        ["NomeCompleto"] = "Nome completo",
        ["Nif"] = "NIF",
        ["Email"] = "E-mail",
        ["Telefone"] = "Telefone",
        ["Telemovel"] = "Telemóvel",
        ["Morada"] = "Morada",
        ["CodigoPostal"] = "Código postal",
        ["Localidade"] = "Localidade",
        ["Pais"] = "País",
        ["Observacoes"] = "Observações",
        ["Ativo"] = "Ativo",
        ["Eliminado"] = "Eliminado",
        ["Tipo"] = "Tipo",
        ["Numero"] = "Número",
        ["Matricula"] = "Matrícula",
        ["Vin"] = "VIN",
        ["Marca"] = "Marca",
        ["Modelo"] = "Modelo",
        ["Versao"] = "Versão",
        ["Ano"] = "Ano",
        ["DataPrimeiraMatricula"] = "Data da 1.ª matrícula",
        ["Combustivel"] = "Combustível",
        ["Caixa"] = "Caixa",
        ["Cilindrada"] = "Cilindrada",
        ["Potencia"] = "Potência",
        ["Cor"] = "Cor",
        ["Quilometragem"] = "Quilometragem",
        ["ProximaInspecao"] = "Próxima inspeção",
        ["ClienteId"] = "Cliente",
        ["Funcao"] = "Função",
        ["Principal"] = "Contacto principal",
        ["UserName"] = "Utilizador",
        ["Cargo"] = "Cargo",
        ["PhoneNumber"] = "Telefone",
        ["TwoFactorEnabled"] = "Autenticação em dois passos",
        ["DeveAlterarPalavraPasse"] = "Tem de alterar a palavra-passe",
        ["Descricao"] = "Descrição",
        ["LockoutEnabled"] = "Bloqueio automático"
    };

    public static string NomeEntidade(string entidade)
        => NomesEntidades.TryGetValue(entidade, out var nome) ? nome : entidade;

    public static string NomeCampo(string campo)
        => NomesCampos.TryGetValue(campo, out var nome) ? nome : campo;

    /// <summary>
    /// Converte um valor guardado no histórico para algo legível: as datas estão gravadas em
    /// formato ISO (para serem comparáveis) e os enums pelo nome técnico. Nenhum dos dois pode
    /// aparecer assim no ecrã — "2026-08-20" e "Gasoleo" não são português.
    /// </summary>
    public static string ValorLegivel(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "(vazio)";

        if (DateOnly.TryParseExact(valor, "yyyy-MM-dd", out var data))
        {
            return Formatos.Data(data);
        }

        if (DateTimeOffset.TryParse(valor, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var instante))
        {
            return Formatos.DataHora(instante);
        }

        foreach (var tipo in new[] { typeof(TipoCliente), typeof(TipoCombustivel), typeof(TipoCaixa) })
        {
            if (Enum.TryParse(tipo, valor, ignoreCase: false, out var enumerado) && enumerado is Enum e)
            {
                return Formatos.Enumeracao(e);
            }
        }

        return valor;
    }

    public static string NomeAcao(AcaoAuditoria acao) => acao switch
    {
        AcaoAuditoria.Criacao => "Criação",
        AcaoAuditoria.Alteracao => "Alteração",
        AcaoAuditoria.Eliminacao => "Eliminação",
        AcaoAuditoria.Reativacao => "Reativação",
        AcaoAuditoria.Autenticacao => "Início de sessão",
        _ => acao.ToString()
    };

    private static readonly MapaOrdenacao<RegistoAuditoria> Ordenacao =
        new MapaOrdenacao<RegistoAuditoria>("instante", q => q.ThenByDescending(r => r.Id))
            .Adicionar("instante", r => r.Instante)
            .Adicionar("entidade", r => r.Entidade)
            .Adicionar("acao", r => r.Acao)
            .Adicionar("utilizador", r => r.UtilizadorNome);

    private readonly AppDbContext _db;
    private readonly IRelogio _relogio;

    public HistoricoServico(AppDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task<ResultadoPaginado<HistoricoLinha>> ObterPaginaAsync(HistoricoFiltro filtro, CancellationToken ct = default)
    {
        var query = _db.RegistosAuditoria.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Entidade))
        {
            query = query.Where(r => r.Entidade == filtro.Entidade);
        }

        if (!string.IsNullOrWhiteSpace(filtro.EntidadeId))
        {
            query = query.Where(r => r.EntidadeId == filtro.EntidadeId);
        }

        if (filtro.Acao.HasValue) query = query.Where(r => r.Acao == filtro.Acao.Value);
        if (filtro.UtilizadorId.HasValue) query = query.Where(r => r.UtilizadorId == filtro.UtilizadorId.Value);

        if (filtro.De.HasValue)
        {
            var de = _relogio.InicioDoDiaUtc(filtro.De.Value);
            query = query.Where(r => r.Instante >= de);
        }

        if (filtro.Ate.HasValue)
        {
            // Inclusivo: o dia escolhido conta por inteiro, da meia-noite local à meia-noite seguinte.
            var ate = _relogio.InicioDoDiaUtc(filtro.Ate.Value.AddDays(1));
            query = query.Where(r => r.Instante < ate);
        }

        var termo = Pesquisa.Limpar(filtro.Pesquisa);
        if (termo is not null)
        {
            var padrao = Pesquisa.ParaPadrao(termo);
            query = query.Where(r =>
                (r.EntidadeDescricao != null && EF.Functions.ILike(r.EntidadeDescricao, padrao))
                || (r.UtilizadorNome != null && EF.Functions.ILike(r.UtilizadorNome, padrao))
                || (r.AlteracoesJson != null && EF.Functions.ILike(r.AlteracoesJson, padrao)));
        }

        var ordenada = Ordenacao.Aplicar(query, filtro.Ordenar, filtro.Direcao is null || filtro.Descendente);
        var pagina = await ordenada.ParaPaginaAsync(filtro, ct);

        var linhas = pagina.Itens.Select(Converter).ToList();
        return new ResultadoPaginado<HistoricoLinha>(linhas, pagina.TotalItens, pagina.Pagina, pagina.TamanhoPagina);
    }

    /// <summary>Histórico de um registo concreto, para a ficha do cliente ou do veículo.</summary>
    public async Task<List<HistoricoLinha>> ObterDoRegistoAsync(
        string entidade,
        string entidadeId,
        int limite = 100,
        CancellationToken ct = default)
    {
        var registos = await _db.RegistosAuditoria.AsNoTracking()
            .Where(r => r.Entidade == entidade && r.EntidadeId == entidadeId)
            .OrderByDescending(r => r.Instante)
            .ThenByDescending(r => r.Id)
            .Take(limite)
            .ToListAsync(ct);

        return registos.Select(Converter).ToList();
    }

    /// <summary>Entidades presentes no histórico, para alimentar o filtro.</summary>
    public Task<List<string>> ObterEntidadesAsync(CancellationToken ct = default)
        => _db.RegistosAuditoria.AsNoTracking()
            .Select(r => r.Entidade)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(ct);

    /// <summary>Registo explícito, para eventos que não passam por uma gravação de entidade (ex.: início de sessão).</summary>
    public async Task RegistarAsync(RegistoAuditoria registo, CancellationToken ct = default)
    {
        _db.RegistosAuditoria.Add(registo);
        await _db.SaveChangesAsync(ct);
    }

    private static HistoricoLinha Converter(RegistoAuditoria r)
    {
        var alteracoes = Array.Empty<AlteracaoCampo>() as IReadOnlyList<AlteracaoCampo>;

        if (!string.IsNullOrWhiteSpace(r.AlteracoesJson))
        {
            try
            {
                alteracoes = JsonSerializer.Deserialize<List<AlteracaoCampo>>(r.AlteracoesJson)
                             ?? new List<AlteracaoCampo>();
            }
            catch (JsonException)
            {
                // Uma linha de histórico corrompida não pode derrubar a página inteira.
                alteracoes = Array.Empty<AlteracaoCampo>();
            }
        }

        return new HistoricoLinha
        {
            Id = r.Id,
            Instante = r.Instante,
            Entidade = r.Entidade,
            EntidadeId = r.EntidadeId,
            EntidadeDescricao = r.EntidadeDescricao,
            Acao = r.Acao,
            UtilizadorNome = r.UtilizadorNome,
            Origem = r.Origem,
            Alteracoes = alteracoes
        };
    }
}
