namespace SmeCore.Domain.Common;

/// <summary>
/// Resultado de uma operação de escrita. Os erros de negócio (NIF duplicado, matrícula já
/// registada) vêm por aqui e são mostrados no formulário — não são exceções.
/// </summary>
public class Resultado
{
    private readonly List<ErroValidacao> _erros = new();

    public bool Sucesso => _erros.Count == 0;

    public IReadOnlyList<ErroValidacao> Erros => _erros;

    public static Resultado Ok() => new();

    public static Resultado Erro(string mensagem, string? campo = null)
    {
        var r = new Resultado();
        r.Acrescentar(mensagem, campo);
        return r;
    }

    public Resultado Acrescentar(string mensagem, string? campo = null)
    {
        _erros.Add(new ErroValidacao(campo, mensagem));
        return this;
    }
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; private init; }

    public static Resultado<T> Ok(T valor) => new() { Valor = valor };

    public static new Resultado<T> Erro(string mensagem, string? campo = null)
    {
        var r = new Resultado<T>();
        r.Acrescentar(mensagem, campo);
        return r;
    }
}

/// <param name="Campo">Nome da propriedade a que o erro pertence, ou null se for geral.</param>
public record ErroValidacao(string? Campo, string Mensagem);
