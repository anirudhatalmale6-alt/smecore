using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Common;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Infrastructure.Dados;

/// <summary>
/// Escreve o histórico automaticamente em cada gravação e preenche os campos de auditoria
/// das entidades. Nenhuma página precisa de se lembrar de registar nada — é por isso que o
/// histórico é fiável.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Propriedades que nunca entram no histórico: segredos do Identity e os próprios campos
    /// de auditoria, que só acrescentariam ruído.
    /// </summary>
    private static readonly HashSet<string> PropriedadesExcluidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "NormalizedUserName",
        "NormalizedEmail", "AccessFailedCount", "LockoutEnd",
        // "Eliminado" não entra: a própria ação (Eliminação/Reativação) já o diz.
        "Eliminado",
        "CriadoEm", "CriadoPorId", "CriadoPorNome",
        "AlteradoEm", "AlteradoPorId", "AlteradoPorNome",
        "EliminadoEm", "EliminadoPorId", "EliminadoPorNome",
        "UltimoAcessoEm"
    };

    /// <summary>
    /// O JSON é gravado com os acentos tal como são escritos. Com o escape predefinido,
    /// "Guimarães" ficaria "Guimar\u00E3es" na base de dados e a pesquisa no histórico
    /// (que corre em ILIKE sobre esta coluna) nunca o encontraria.
    /// </summary>
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IUtilizadorAtual _utilizador;
    private readonly IRelogio _relogio;

    public AuditoriaInterceptor(IUtilizadorAtual utilizador, IRelogio relogio)
    {
        _utilizador = utilizador;
        _relogio = relogio;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Processar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Processar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Processar(DbContext? contexto)
    {
        if (contexto is null) return;

        contexto.ChangeTracker.DetectChanges();

        var agora = _relogio.AgoraUtc;
        var registos = new List<RegistoAuditoria>();

        foreach (var entrada in contexto.ChangeTracker.Entries().ToList())
        {
            // O próprio histórico nunca é auditado, sob pena de recursão infinita.
            if (entrada.Entity is RegistoAuditoria) continue;
            if (!EhAuditavel(entrada.Entity)) continue;

            switch (entrada.State)
            {
                case EntityState.Added:
                    AplicarCriacao(entrada, agora);
                    registos.Add(Construir(entrada, AcaoAuditoria.Criacao, agora, ValoresIniciais(entrada)));
                    break;

                case EntityState.Modified:
                {
                    var alteracoes = Diferencas(entrada);
                    var acao = ClassificarAlteracao(entrada);

                    AplicarAlteracao(entrada, agora, acao);

                    // Uma gravação que não mexeu em nenhum campo relevante não gera histórico.
                    if (alteracoes.Count == 0 && acao == AcaoAuditoria.Alteracao) break;

                    registos.Add(Construir(entrada, acao, agora, alteracoes));
                    break;
                }

                case EntityState.Deleted:
                    registos.Add(Construir(entrada, AcaoAuditoria.Eliminacao, agora, new List<AlteracaoCampo>()));
                    break;
            }
        }

        if (registos.Count > 0)
        {
            contexto.Set<RegistoAuditoria>().AddRange(registos);
        }
    }

    private static bool EhAuditavel(object entidade)
        => entidade is EntidadeAuditavel or Utilizador or Perfil;

    private void AplicarCriacao(EntityEntry entrada, DateTimeOffset agora)
    {
        if (entrada.Entity is EntidadeAuditavel auditavel)
        {
            auditavel.CriadoEm = agora;
            auditavel.CriadoPorId = _utilizador.Id;
            auditavel.CriadoPorNome = _utilizador.Nome;
        }
        else if (entrada.Entity is Utilizador utilizador && utilizador.CriadoEm == default)
        {
            utilizador.CriadoEm = agora;
        }
    }

    private void AplicarAlteracao(EntityEntry entrada, DateTimeOffset agora, AcaoAuditoria acao)
    {
        if (entrada.Entity is not EntidadeAuditavel auditavel) return;

        auditavel.AlteradoEm = agora;
        auditavel.AlteradoPorId = _utilizador.Id;
        auditavel.AlteradoPorNome = _utilizador.Nome;

        if (acao == AcaoAuditoria.Eliminacao)
        {
            auditavel.EliminadoEm = agora;
            auditavel.EliminadoPorId = _utilizador.Id;
            auditavel.EliminadoPorNome = _utilizador.Nome;
        }
        else if (acao == AcaoAuditoria.Reativacao)
        {
            auditavel.EliminadoEm = null;
            auditavel.EliminadoPorId = null;
            auditavel.EliminadoPorNome = null;
        }
    }

    private static AcaoAuditoria ClassificarAlteracao(EntityEntry entrada)
    {
        var propriedade = entrada.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(EntidadeAuditavel.Eliminado));
        if (propriedade is null || !propriedade.IsModified) return AcaoAuditoria.Alteracao;

        var antes = propriedade.OriginalValue as bool? ?? false;
        var depois = propriedade.CurrentValue as bool? ?? false;

        if (!antes && depois) return AcaoAuditoria.Eliminacao;
        if (antes && !depois) return AcaoAuditoria.Reativacao;
        return AcaoAuditoria.Alteracao;
    }

    private static List<AlteracaoCampo> Diferencas(EntityEntry entrada)
    {
        var lista = new List<AlteracaoCampo>();

        foreach (var propriedade in entrada.Properties)
        {
            if (!propriedade.IsModified) continue;
            if (propriedade.Metadata.IsPrimaryKey()) continue;
            if (PropriedadesExcluidas.Contains(propriedade.Metadata.Name)) continue;

            var antes = ParaTexto(propriedade.OriginalValue);
            var depois = ParaTexto(propriedade.CurrentValue);
            if (antes == depois) continue;

            lista.Add(new AlteracaoCampo(propriedade.Metadata.Name, antes, depois));
        }

        return lista;
    }

    private static List<AlteracaoCampo> ValoresIniciais(EntityEntry entrada)
    {
        var lista = new List<AlteracaoCampo>();

        foreach (var propriedade in entrada.Properties)
        {
            if (propriedade.Metadata.IsPrimaryKey()) continue;
            if (PropriedadesExcluidas.Contains(propriedade.Metadata.Name)) continue;

            // Colunas preenchidas pela base de dados (sequências, por exemplo) ainda valem 0
            // neste momento: registá-las escreveria "Número: 0" no histórico de criação.
            if (propriedade.Metadata.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never) continue;

            var valor = ParaTexto(propriedade.CurrentValue);
            if (string.IsNullOrEmpty(valor)) continue;

            lista.Add(new AlteracaoCampo(propriedade.Metadata.Name, null, valor));
        }

        return lista;
    }

    private RegistoAuditoria Construir(
        EntityEntry entrada,
        AcaoAuditoria acao,
        DateTimeOffset agora,
        List<AlteracaoCampo> alteracoes)
    {
        return new RegistoAuditoria
        {
            Instante = agora,
            Entidade = entrada.Metadata.ClrType.Name,
            EntidadeId = ChavePrimaria(entrada),
            EntidadeDescricao = Descrever(entrada.Entity),
            Acao = acao,
            UtilizadorId = _utilizador.Id,
            UtilizadorNome = _utilizador.Nome,
            Origem = _utilizador.Origem,
            AlteracoesJson = alteracoes.Count == 0 ? null : JsonSerializer.Serialize(alteracoes, OpcoesJson)
        };
    }

    private static string ChavePrimaria(EntityEntry entrada)
    {
        var chaves = entrada.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? string.Empty)
            .ToList();

        return chaves.Count == 0 ? string.Empty : string.Join("|", chaves);
    }

    /// <summary>Descrição legível do registo, para o histórico continuar a fazer sentido depois de o registo mudar.</summary>
    private static string? Descrever(object entidade) => entidade switch
    {
        Cliente c => c.Nome,
        ClienteContacto ct => $"Contacto: {ct.Nome}",
        Veiculo v => $"{v.Matricula} — {v.Marca} {v.Modelo}".Trim(),
        Utilizador u => $"{u.NomeApresentacao} ({u.Email})",
        Perfil p => $"Perfil: {p.Name}",
        _ => null
    };

    private static string? ParaTexto(object? valor) => valor switch
    {
        null => null,
        bool b => b ? "Sim" : "Não",
        DateTimeOffset d => d.ToString("O"),
        DateOnly d => d.ToString("yyyy-MM-dd"),
        Enum e => e.ToString(),
        _ => valor.ToString()
    };
}
