namespace SmeCore.Infrastructure.Identidade;

/// <summary>
/// Quem está a fazer a operação. Implementado no projeto Web a partir do HttpContext;
/// em testes e em tarefas de arranque usa-se <see cref="UtilizadorSistema"/>.
/// </summary>
public interface IUtilizadorAtual
{
    Guid? Id { get; }
    string? Nome { get; }

    /// <summary>Endereço IP de origem, quando existe um pedido HTTP.</summary>
    string? Origem { get; }
}

/// <summary>Identidade usada por migrações, seed e tarefas automáticas.</summary>
public sealed class UtilizadorSistema : IUtilizadorAtual
{
    public Guid? Id => null;
    public string? Nome => "Sistema";
    public string? Origem => null;
}
