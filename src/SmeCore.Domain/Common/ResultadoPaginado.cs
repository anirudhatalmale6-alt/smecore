namespace SmeCore.Domain.Common;

/// <summary>
/// Página de resultados devolvida por qualquer listagem. Genérico de propósito: todos os
/// módulos futuros (orçamentos, obras, stock) devolvem este mesmo tipo.
/// </summary>
public class ResultadoPaginado<T>
{
    public IReadOnlyList<T> Itens { get; }

    /// <summary>Total de registos que satisfazem os filtros, ignorando a paginação.</summary>
    public int TotalItens { get; }

    /// <summary>Página atual, a começar em 1.</summary>
    public int Pagina { get; }

    public int TamanhoPagina { get; }

    public ResultadoPaginado(IReadOnlyList<T> itens, int totalItens, int pagina, int tamanhoPagina)
    {
        Itens = itens;
        TotalItens = totalItens;
        Pagina = pagina < 1 ? 1 : pagina;
        TamanhoPagina = tamanhoPagina < 1 ? 1 : tamanhoPagina;
    }

    public int TotalPaginas => TotalItens == 0 ? 1 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);

    public bool TemAnterior => Pagina > 1;
    public bool TemSeguinte => Pagina < TotalPaginas;

    /// <summary>Índice do primeiro item da página, a começar em 1 (0 quando não há resultados).</summary>
    public int PrimeiroItem => TotalItens == 0 ? 0 : ((Pagina - 1) * TamanhoPagina) + 1;

    /// <summary>Índice do último item da página.</summary>
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalItens);

    public static ResultadoPaginado<T> Vazio(int tamanhoPagina = 25)
        => new(Array.Empty<T>(), 0, 1, tamanhoPagina);
}

/// <summary>Parâmetros comuns a todas as listagens: pesquisa, ordenação e paginação.</summary>
public class ParametrosListagem
{
    public const int TamanhoPaginaPredefinido = 25;

    /// <summary>Tamanhos oferecidos no seletor "por página".</summary>
    public static readonly int[] TamanhosPagina = { 10, 25, 50, 100 };

    public const int TamanhoPaginaMaximo = 200;

    private int _pagina = 1;
    private int _tamanhoPagina = TamanhoPaginaPredefinido;

    /// <summary>Texto livre de pesquisa.</summary>
    public string? Pesquisa { get; set; }

    /// <summary>Nome do campo de ordenação. Cada listagem define os campos que aceita.</summary>
    public string? Ordenar { get; set; }

    /// <summary>"asc" ou "desc". Qualquer outro valor é tratado como "asc".</summary>
    public string? Direcao { get; set; }

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanhoPagina
    {
        get => _tamanhoPagina;
        set => _tamanhoPagina = value < 1
            ? TamanhoPaginaPredefinido
            : Math.Min(value, TamanhoPaginaMaximo);
    }

    public bool Descendente => string.Equals(Direcao, "desc", StringComparison.OrdinalIgnoreCase);

    public int Saltar => (Pagina - 1) * TamanhoPagina;

    /// <summary>Direção oposta à atual, para construir os links de cabeçalho de coluna.</summary>
    public string DirecaoAlternada(string campo)
    {
        var mesmoCampo = string.Equals(Ordenar, campo, StringComparison.OrdinalIgnoreCase);
        if (!mesmoCampo) return "asc";
        return Descendente ? "asc" : "desc";
    }
}
