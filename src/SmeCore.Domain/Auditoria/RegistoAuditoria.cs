namespace SmeCore.Domain.Auditoria;

public enum AcaoAuditoria
{
    Criacao = 1,
    Alteracao = 2,
    Eliminacao = 3,
    Reativacao = 4,
    Autenticacao = 5
}

/// <summary>
/// Linha do histórico. Escrita automaticamente pelo interceptor de auditoria em cada
/// gravação, nunca à mão nas páginas. Imutável: só se insere, nunca se altera.
/// </summary>
public class RegistoAuditoria
{
    public long Id { get; set; }

    public DateTimeOffset Instante { get; set; }

    /// <summary>Nome da entidade afetada, tal como registada no modelo (ex.: "Cliente").</summary>
    public string Entidade { get; set; } = string.Empty;

    /// <summary>Chave primária do registo afetado, em texto (suporta Guid, int e chaves compostas).</summary>
    public string EntidadeId { get; set; } = string.Empty;

    /// <summary>Descrição legível do registo no momento da operação (ex.: "AA-00-AA — Renault Clio").</summary>
    public string? EntidadeDescricao { get; set; }

    public AcaoAuditoria Acao { get; set; }

    public Guid? UtilizadorId { get; set; }
    public string? UtilizadorNome { get; set; }

    /// <summary>Endereço IP de origem, quando disponível no pedido HTTP.</summary>
    public string? Origem { get; set; }

    /// <summary>Alterações em JSON: [{ "campo": "Nome", "de": "...", "para": "..." }].</summary>
    public string? AlteracoesJson { get; set; }
}

/// <summary>Par antes/depois de um campo, serializado em <see cref="RegistoAuditoria.AlteracoesJson"/>.</summary>
public record AlteracaoCampo(string Campo, string? De, string? Para);
