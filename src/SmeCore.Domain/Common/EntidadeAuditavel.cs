namespace SmeCore.Domain.Common;

/// <summary>
/// Base de todas as entidades de negócio. Guarda quem criou/alterou e quando,
/// e suporta eliminação lógica para que o histórico nunca perca a referência.
/// </summary>
public abstract class EntidadeAuditavel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Instante de criação, sempre em UTC.</summary>
    public DateTimeOffset CriadoEm { get; set; }

    /// <summary>Id do utilizador que criou o registo (null quando criado pelo sistema/seed).</summary>
    public Guid? CriadoPorId { get; set; }

    /// <summary>Nome de quem criou, desnormalizado para o histórico sobreviver à remoção do utilizador.</summary>
    public string? CriadoPorNome { get; set; }

    public DateTimeOffset? AlteradoEm { get; set; }
    public Guid? AlteradoPorId { get; set; }
    public string? AlteradoPorNome { get; set; }

    /// <summary>Eliminação lógica: o registo deixa de aparecer nas listagens mas continua no histórico.</summary>
    public bool Eliminado { get; set; }
    public DateTimeOffset? EliminadoEm { get; set; }
    public Guid? EliminadoPorId { get; set; }
    public string? EliminadoPorNome { get; set; }
}
