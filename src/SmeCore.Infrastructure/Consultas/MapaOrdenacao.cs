using System.Linq.Expressions;

namespace SmeCore.Infrastructure.Consultas;

/// <summary>
/// Lista branca de campos pelos quais uma listagem pode ser ordenada. Evita que o nome de
/// coluna venha da query string para dentro da consulta, e garante que a ordenação é sempre
/// determinística — sem um critério de desempate, a paginação repete ou salta registos.
/// </summary>
public sealed class MapaOrdenacao<T>
{
    private readonly Dictionary<string, Func<IQueryable<T>, bool, IOrderedQueryable<T>>> _campos =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Func<IOrderedQueryable<T>, IOrderedQueryable<T>> _desempate;

    public string CampoPredefinido { get; }

    /// <param name="campoPredefinido">Campo usado quando o pedido não indica nenhum (ou indica um inválido).</param>
    /// <param name="desempate">Critério final de ordenação, normalmente a chave primária.</param>
    public MapaOrdenacao(
        string campoPredefinido,
        Func<IOrderedQueryable<T>, IOrderedQueryable<T>> desempate)
    {
        CampoPredefinido = campoPredefinido;
        _desempate = desempate;
    }

    public MapaOrdenacao<T> Adicionar<TChave>(string campo, Expression<Func<T, TChave>> seletor)
    {
        _campos[campo] = (query, descendente) => descendente
            ? query.OrderByDescending(seletor)
            : query.OrderBy(seletor);
        return this;
    }

    /// <summary>Acrescenta um campo cuja ordenação envolve mais do que uma coluna.</summary>
    public MapaOrdenacao<T> Adicionar<TChave1, TChave2>(
        string campo,
        Expression<Func<T, TChave1>> primeiro,
        Expression<Func<T, TChave2>> segundo)
    {
        _campos[campo] = (query, descendente) => descendente
            ? query.OrderByDescending(primeiro).ThenByDescending(segundo)
            : query.OrderBy(primeiro).ThenBy(segundo);
        return this;
    }

    public bool Suporta(string? campo)
        => !string.IsNullOrWhiteSpace(campo) && _campos.ContainsKey(campo);

    /// <summary>Nome do campo que vai ser efetivamente usado (o pedido ou o predefinido).</summary>
    public string Resolver(string? campo)
        => Suporta(campo) ? campo! : CampoPredefinido;

    public IQueryable<T> Aplicar(IQueryable<T> query, string? campo, bool descendente)
    {
        var escolhido = Resolver(campo);
        var ordenador = _campos[escolhido];
        return _desempate(ordenador(query, descendente));
    }
}
