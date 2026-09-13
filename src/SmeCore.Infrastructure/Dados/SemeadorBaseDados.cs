using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Infrastructure.Dados;

/// <summary>
/// Prepara a base de dados no arranque: perfis de sistema, permissões predefinidas e a conta
/// de administrador inicial. Os dados de demonstração são opcionais e nunca correm por omissão.
/// </summary>
public class SemeadorBaseDados
{
    private readonly AppDbContext _db;
    private readonly UserManager<Utilizador> _utilizadores;
    private readonly RoleManager<Perfil> _perfis;
    private readonly IPermissoesServico _permissoes;
    private readonly IConfiguration _configuracao;
    private readonly IRelogio _relogio;
    private readonly ILogger<SemeadorBaseDados> _log;

    public SemeadorBaseDados(
        AppDbContext db,
        UserManager<Utilizador> utilizadores,
        RoleManager<Perfil> perfis,
        IPermissoesServico permissoes,
        IConfiguration configuracao,
        IRelogio relogio,
        ILogger<SemeadorBaseDados> log)
    {
        _db = db;
        _utilizadores = utilizadores;
        _perfis = perfis;
        _permissoes = permissoes;
        _configuracao = configuracao;
        _relogio = relogio;
        _log = log;
    }

    public async Task ExecutarAsync(CancellationToken ct = default)
    {
        await GarantirPerfisAsync();
        await GarantirAdministradorAsync();

        if (_configuracao.GetValue("Seed:DadosDemonstracao", false))
        {
            await GarantirDadosDemonstracaoAsync(ct);
        }
    }

    private async Task GarantirPerfisAsync()
    {
        var definicoes = new[]
        {
            (Nome: Permissoes.PerfilAdministrador,
             Descricao: "Acesso total, incluindo gestão de utilizadores e permissões.",
             Permissoes: (IReadOnlyList<string>)Permissoes.Todas),
            (Nome: Permissoes.PerfilGestor,
             Descricao: "Gere clientes e veículos e consulta o histórico.",
             Permissoes: Permissoes.PredefinidasGestor),
            (Nome: Permissoes.PerfilUtilizador,
             Descricao: "Registo e consulta do dia a dia, sem eliminar nem exportar.",
             Permissoes: Permissoes.PredefinidasUtilizador)
        };

        foreach (var definicao in definicoes)
        {
            var perfil = await _perfis.FindByNameAsync(definicao.Nome);
            if (perfil is null)
            {
                perfil = new Perfil(definicao.Nome)
                {
                    Descricao = definicao.Descricao,
                    DeSistema = true
                };

                var criacao = await _perfis.CreateAsync(perfil);
                if (!criacao.Succeeded)
                {
                    _log.LogError("Não foi possível criar o perfil {Perfil}: {Erros}",
                        definicao.Nome, string.Join("; ", criacao.Errors.Select(e => e.Description)));
                    continue;
                }

                // As permissões só são escritas na criação. Depois disso, quem manda é o que o
                // administrador definiu no ecrã de perfis — um reinício não desfaz o trabalho dele.
                if (definicao.Nome != Permissoes.PerfilAdministrador)
                {
                    await _permissoes.DefinirPermissoesDoPerfilAsync(perfil, definicao.Permissoes);
                }

                _log.LogInformation("Perfil {Perfil} criado.", definicao.Nome);
            }
            else if (!perfil.DeSistema)
            {
                perfil.DeSistema = true;
                await _perfis.UpdateAsync(perfil);
            }
        }
    }

    private async Task GarantirAdministradorAsync()
    {
        var email = _configuracao["Administrador:Email"];
        var palavraPasse = _configuracao["Administrador:PalavraPasse"];
        var nome = _configuracao["Administrador:Nome"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email))
        {
            // Sem conta inicial configurada não se inventa nenhuma: uma conta com palavra-passe
            // previsível num sistema acessível pela Internet é pior do que não haver conta.
            var existeAlgum = await _db.Users.AnyAsync();
            if (!existeAlgum)
            {
                _log.LogWarning(
                    "Não existe nenhum utilizador e Administrador:Email não está definido. " +
                    "Defina Administrador__Email e Administrador__PalavraPasse para criar a conta inicial.");
            }
            return;
        }

        var utilizador = await _utilizadores.FindByEmailAsync(email);
        if (utilizador is not null)
        {
            // A conta já existe: garante-se apenas que continua administradora e ativa.
            if (!await _utilizadores.IsInRoleAsync(utilizador, Permissoes.PerfilAdministrador))
            {
                await _utilizadores.AddToRoleAsync(utilizador, Permissoes.PerfilAdministrador);
                _log.LogInformation("Conta {Email} promovida a administrador.", email);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(palavraPasse))
        {
            _log.LogWarning(
                "Administrador:Email está definido ({Email}) mas Administrador:PalavraPasse não. " +
                "A conta inicial não foi criada.", email);
            return;
        }

        utilizador = new Utilizador
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NomeCompleto = nome,
            Cargo = "Administração",
            Ativo = true,
            CriadoEm = _relogio.AgoraUtc
        };

        var resultado = await _utilizadores.CreateAsync(utilizador, palavraPasse);
        if (!resultado.Succeeded)
        {
            _log.LogError("Não foi possível criar a conta inicial {Email}: {Erros}",
                email, string.Join("; ", resultado.Errors.Select(e => e.Description)));
            return;
        }

        await _utilizadores.AddToRoleAsync(utilizador, Permissoes.PerfilAdministrador);
        _log.LogInformation("Conta de administrador {Email} criada.", email);
    }

    /// <summary>
    /// Dados de demonstração: clientes e veículos com NIF e matrículas válidos, e datas de
    /// inspeção distribuídas em torno da data atual (algumas vencidas, outras próximas, outras
    /// distantes) para que os filtros tenham sempre resultados, seja em que dia for.
    /// </summary>
    private async Task GarantirDadosDemonstracaoAsync(CancellationToken ct)
    {
        if (await _db.Clientes.AnyAsync(ct))
        {
            _log.LogInformation("Dados de demonstração ignorados: já existem clientes.");
            return;
        }

        var aleatorio = new Random(20260913); // semente fixa: o mesmo conjunto em cada ambiente
        var hoje = _relogio.HojeLocal;

        var nomes = new[]
        {
            "Ana Ribeiro", "Bruno Carvalho", "Carla Mendes", "Diogo Oliveira", "Eduarda Faria",
            "Fábio Sousa", "Gabriela Pinto", "Hugo Antunes", "Inês Marques", "João Baptista",
            "Liliana Rocha", "Manuel Teixeira", "Nuno Correia", "Olga Ferreira", "Pedro Nogueira",
            "Raquel Lopes", "Sérgio Domingues", "Teresa Amaral", "Vítor Castro", "Zélia Moreira"
        };

        var empresas = new[]
        {
            "Transportes Vale do Ave, Lda.", "Padaria Central de Braga, Unipessoal Lda.",
            "Construções Serra Norte, S.A.", "Frota Atlântica, Lda.", "Oficina Irmãos Sá, Lda.",
            "Distribuições Minho Verde, Lda.", "Táxis Cidade Berço, Lda.", "Clínica Bem-Estar, Lda."
        };

        var localidades = new[]
        {
            "Braga", "Guimarães", "Porto", "Barcelos", "Famalicão", "Viana do Castelo", "Fafe", "Póvoa de Varzim"
        };

        var modelos = new (string Marca, string Modelo, string Versao, TipoCombustivel Combustivel, int Cilindrada, int Potencia, int DesdeAno)[]
        {
            ("Renault", "Clio", "1.5 dCi Dynamique", TipoCombustivel.Gasoleo, 1461, 90, 2005),
            ("Renault", "Mégane", "1.3 TCe Intens", TipoCombustivel.Gasolina, 1332, 140, 2018),
            ("Peugeot", "208", "1.2 PureTech Allure", TipoCombustivel.Gasolina, 1199, 100, 2012),
            ("Peugeot", "3008", "1.5 BlueHDi GT Line", TipoCombustivel.Gasoleo, 1499, 130, 2017),
            ("Volkswagen", "Golf", "2.0 TDI Confortline", TipoCombustivel.Gasoleo, 1968, 150, 2008),
            ("Volkswagen", "Polo", "1.0 TSI Highline", TipoCombustivel.Gasolina, 999, 95, 2015),
            ("Mercedes-Benz", "Sprinter", "314 CDI", TipoCombustivel.Gasoleo, 2143, 143, 2014),
            ("Toyota", "Corolla", "1.8 Hybrid Comfort", TipoCombustivel.HibridoGasolina, 1798, 122, 2019),
            ("Nissan", "Qashqai", "1.3 DIG-T N-Connecta", TipoCombustivel.Gasolina, 1332, 140, 2019),
            ("BMW", "Série 3", "320d Pack M", TipoCombustivel.Gasoleo, 1995, 190, 2012),
            ("Tesla", "Model 3", "Long Range AWD", TipoCombustivel.Eletrico, 0, 440, 2019),
            ("Fiat", "Ducato", "2.3 MultiJet 35 L4H2", TipoCombustivel.Gasoleo, 2287, 140, 2010),
            ("Opel", "Corsa", "1.2 Edition", TipoCombustivel.Gasolina, 1199, 75, 2007),
            ("Citroën", "Berlingo", "1.5 BlueHDi Van", TipoCombustivel.Gasoleo, 1499, 100, 2018),
            ("Dacia", "Duster", "1.0 TCe GPL Comfort", TipoCombustivel.Gpl, 999, 100, 2020)
        };

        var cores = new[] { "Branco", "Cinzento", "Preto", "Azul-escuro", "Vermelho", "Prata" };

        var clientes = new List<Cliente>();

        // O NIF tem índice único: um valor repetido faria a gravação falhar no arranque.
        var nifsUsados = new HashSet<string>();
        string NifUnico(char primeiro)
        {
            string nif;
            do
            {
                nif = GerarNif(aleatorio, primeiro);
            }
            while (!nifsUsados.Add(nif));
            return nif;
        }

        for (var i = 0; i < nomes.Length; i++)
        {
            var localidade = localidades[i % localidades.Length];
            clientes.Add(new Cliente
            {
                Tipo = TipoCliente.Particular,
                Nome = nomes[i],
                Nif = NifUnico('2'),
                Email = $"{SemAcentos(nomes[i]).Replace(' ', '.').ToLowerInvariant()}@exemplo.pt",
                Telemovel = $"9{aleatorio.Next(1, 4)}{aleatorio.Next(1000000, 9999999)}",
                Morada = $"Rua das Flores, n.º {aleatorio.Next(1, 220)}",
                CodigoPostal = $"4{aleatorio.Next(100, 999)}-{aleatorio.Next(100, 999)}",
                Localidade = localidade,
                Ativo = i % 11 != 0
            });
        }

        foreach (var empresa in empresas)
        {
            var localidade = localidades[aleatorio.Next(localidades.Length)];
            var cliente = new Cliente
            {
                Tipo = TipoCliente.Empresa,
                Nome = empresa,
                Nif = NifUnico('5'),
                Email = $"geral@{SemAcentos(empresa.Split(',')[0]).Replace(" ", "").ToLowerInvariant()}.pt",
                Telefone = $"2{aleatorio.Next(10, 99)}{aleatorio.Next(100000, 999999)}",
                Morada = $"Zona Industrial, Lote {aleatorio.Next(1, 40)}",
                CodigoPostal = $"4{aleatorio.Next(100, 999)}-{aleatorio.Next(100, 999)}",
                Localidade = localidade,
                Observacoes = "Faturação mensal. Contacto preferencial por e-mail.",
                Ativo = true
            };

            cliente.Contactos.Add(new ClienteContacto
            {
                Nome = nomes[aleatorio.Next(nomes.Length)],
                Funcao = "Gestão de frota",
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                Principal = true
            });

            clientes.Add(cliente);
        }

        _db.Clientes.AddRange(clientes);
        await _db.SaveChangesAsync(ct);

        var veiculos = new List<Veiculo>();
        var matriculasUsadas = new HashSet<string>();

        foreach (var cliente in clientes)
        {
            var quantos = cliente.Tipo == TipoCliente.Empresa ? aleatorio.Next(2, 6) : aleatorio.Next(0, 3);

            for (var v = 0; v < quantos; v++)
            {
                var modelo = modelos[aleatorio.Next(modelos.Length)];

                // O ano nunca é anterior ao lançamento do modelo: um "Tesla Model 3 de 2006"
                // faria qualquer pessoa duvidar do resto dos dados.
                var ano = aleatorio.Next(modelo.DesdeAno, hoje.Year + 1);

                string matricula;
                do
                {
                    matricula = GerarMatricula(aleatorio, ano);
                }
                while (!matriculasUsadas.Add(matricula));

                // Inspeções: 15% vencidas, 25% dentro de 30 dias, o resto espalhado pelo ano.
                var sorteio = aleatorio.NextDouble();
                DateOnly? inspecao = sorteio switch
                {
                    < 0.15 => hoje.AddDays(-aleatorio.Next(1, 120)),
                    < 0.40 => hoje.AddDays(aleatorio.Next(0, 31)),
                    < 0.95 => hoje.AddDays(aleatorio.Next(31, 365)),
                    _ => null
                };

                veiculos.Add(new Veiculo
                {
                    ClienteId = cliente.Id,
                    Matricula = matricula,
                    Vin = GerarVin(aleatorio),
                    Marca = modelo.Marca,
                    Modelo = modelo.Modelo,
                    Versao = modelo.Versao,
                    Ano = ano,
                    DataPrimeiraMatricula = new DateOnly(ano, aleatorio.Next(1, 13), aleatorio.Next(1, 28)),
                    Combustivel = modelo.Combustivel,
                    Caixa = aleatorio.Next(0, 3) == 0 ? TipoCaixa.Automatica : TipoCaixa.Manual,
                    Cilindrada = modelo.Cilindrada == 0 ? null : modelo.Cilindrada,
                    Potencia = modelo.Potencia,
                    Cor = cores[aleatorio.Next(cores.Length)],
                    Quilometragem = aleatorio.Next(5, 340) * 1000 + aleatorio.Next(0, 999),
                    ProximaInspecao = inspecao,
                    Ativo = true
                });
            }
        }

        _db.Veiculos.AddRange(veiculos);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Dados de demonstração criados: {Clientes} clientes e {Veiculos} veículos.",
            clientes.Count, veiculos.Count);
    }

    /// <summary>Gera um NIF com dígito de controlo correto, para os dados de demonstração passarem a validação.</summary>
    internal static string GerarNif(Random aleatorio, char primeiro)
    {
        var digitos = new char[9];
        digitos[0] = primeiro;
        for (var i = 1; i < 8; i++)
        {
            digitos[i] = (char)('0' + aleatorio.Next(0, 10));
        }

        var soma = 0;
        for (var i = 0; i < 8; i++)
        {
            soma += (digitos[i] - '0') * (9 - i);
        }

        var resto = soma % 11;
        var controlo = resto < 2 ? 0 : 11 - resto;
        digitos[8] = (char)('0' + controlo);

        return new string(digitos);
    }

    /// <summary>Matrícula no formato correspondente ao ano de matrícula.</summary>
    internal static string GerarMatricula(Random aleatorio, int ano)
    {
        const string letras = "ABCDEFGHJKLMNPQRSTUVXZ";

        string Par() => $"{aleatorio.Next(0, 100):D2}";
        string Letras() => $"{letras[aleatorio.Next(letras.Length)]}{letras[aleatorio.Next(letras.Length)]}";

        return ano switch
        {
            >= 2020 => $"{Letras()}-{Par()}-{Letras()}",
            >= 2005 => $"{Par()}-{Letras()}-{Par()}",
            >= 1992 => $"{Par()}-{Par()}-{Letras()}",
            _ => $"{Letras()}-{Par()}-{Par()}"
        };
    }

    internal static string GerarVin(Random aleatorio)
    {
        // O VIN não usa I, O nem Q, para não se confundirem com 1 e 0.
        const string alfabeto = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        var vin = new char[17];
        for (var i = 0; i < vin.Length; i++)
        {
            vin[i] = alfabeto[aleatorio.Next(alfabeto.Length)];
        }
        return new string(vin);
    }

    private static string SemAcentos(string texto)
    {
        var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
