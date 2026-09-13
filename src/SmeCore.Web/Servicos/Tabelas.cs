using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.WebUtilities;
using SmeCore.Domain.Common;

namespace SmeCore.Web.Servicos;

/// <summary>
/// Cabeçalhos de coluna ordenáveis. Cada listagem só precisa de dizer o nome do campo e o
/// título; os filtros em curso, a direção e o indicador visual são resolvidos aqui.
/// </summary>
public static class Tabelas
{
    public static IHtmlContent CabecalhoOrdenavel(
        HttpRequest pedido,
        string campo,
        string titulo,
        string? classe = null)
    {
        var ordenarAtual = pedido.Query["ordenar"].ToString();
        var direcaoAtual = pedido.Query["direcao"].ToString();

        var ativo = string.Equals(ordenarAtual, campo, StringComparison.OrdinalIgnoreCase);
        var descendente = ativo && string.Equals(direcaoAtual, "desc", StringComparison.OrdinalIgnoreCase);
        var proximaDirecao = ativo && !descendente ? "desc" : "asc";

        var parametros = Ligacoes.Com(
            pedido,
            ("ordenar", campo),
            ("direcao", proximaDirecao),
            ("pagina", null));

        var url = ConstruirUrl(pedido.Path, parametros);

        var seta = ativo ? (descendente ? "▼" : "▲") : string.Empty;
        var titulosAcessiveis = ativo
            ? (descendente ? $"{titulo}, ordenado de forma descendente" : $"{titulo}, ordenado de forma ascendente")
            : $"Ordenar por {titulo}";

        var atributoClasse = string.IsNullOrWhiteSpace(classe) ? string.Empty : $" class=\"{classe}\"";
        var codificado = HtmlEncoder.Default.Encode(titulo);

        return new HtmlString(
            $"<th{atributoClasse} scope=\"col\" aria-sort=\"{(ativo ? (descendente ? "descending" : "ascending") : "none")}\">" +
            $"<a href=\"{HtmlEncoder.Default.Encode(url)}\" title=\"{HtmlEncoder.Default.Encode(titulosAcessiveis)}\">" +
            $"{codificado}{(seta.Length > 0 ? $"<span class=\"seta\" aria-hidden=\"true\">{seta}</span>" : string.Empty)}" +
            "</a></th>");
    }

    internal static string ConstruirUrl(PathString caminho, Dictionary<string, string?> parametros)
    {
        var limpos = parametros
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .ToDictionary(p => p.Key, p => p.Value);

        return QueryHelpers.AddQueryString(caminho.Value ?? "/", limpos);
    }
}

/// <summary>Dados necessários para desenhar a barra de paginação, sem depender do tipo listado.</summary>
public class ModeloPaginacao
{
    public int Pagina { get; init; }
    public int TotalPaginas { get; init; }
    public int TotalItens { get; init; }
    public int PrimeiroItem { get; init; }
    public int UltimoItem { get; init; }
    public int TamanhoPagina { get; init; }
    public string Caminho { get; init; } = "/";
    public Dictionary<string, string?> Parametros { get; init; } = new();

    /// <summary>Palavra usada nos textos ("cliente", "veículo"...), no singular.</summary>
    public string Substantivo { get; init; } = "registo";
    public string SubstantivoPlural { get; init; } = "registos";

    public static ModeloPaginacao De<T>(
        ResultadoPaginado<T> pagina,
        HttpRequest pedido,
        string substantivo,
        string substantivoPlural)
        => new()
        {
            Pagina = pagina.Pagina,
            TotalPaginas = pagina.TotalPaginas,
            TotalItens = pagina.TotalItens,
            PrimeiroItem = pagina.PrimeiroItem,
            UltimoItem = pagina.UltimoItem,
            TamanhoPagina = pagina.TamanhoPagina,
            Caminho = pedido.Path.Value ?? "/",
            Parametros = Ligacoes.Com(pedido),
            Substantivo = substantivo,
            SubstantivoPlural = substantivoPlural
        };

    public string Url(int pagina)
    {
        var parametros = new Dictionary<string, string?>(Parametros, StringComparer.OrdinalIgnoreCase)
        {
            ["pagina"] = pagina.ToString()
        };
        return Tabelas.ConstruirUrl(Caminho, parametros);
    }

    public string UrlTamanho(int tamanho)
    {
        var parametros = new Dictionary<string, string?>(Parametros, StringComparer.OrdinalIgnoreCase)
        {
            ["tamanhoPagina"] = tamanho.ToString()
        };
        parametros.Remove("pagina");
        return Tabelas.ConstruirUrl(Caminho, parametros);
    }

    /// <summary>
    /// Números de página a mostrar: sempre a primeira e a última, e uma janela em volta da
    /// atual. Com 400 páginas, listar todas tornaria a barra inutilizável.
    /// </summary>
    public IReadOnlyList<int?> Janela()
    {
        var paginas = new List<int?>();
        if (TotalPaginas <= 1) return paginas;

        const int raio = 2;
        var inicio = Math.Max(1, Pagina - raio);
        var fim = Math.Min(TotalPaginas, Pagina + raio);

        if (inicio > 1)
        {
            paginas.Add(1);
            if (inicio > 2) paginas.Add(null); // reticências
        }

        for (var p = inicio; p <= fim; p++) paginas.Add(p);

        if (fim < TotalPaginas)
        {
            if (fim < TotalPaginas - 1) paginas.Add(null);
            paginas.Add(TotalPaginas);
        }

        return paginas;
    }

    public string Resumo()
    {
        if (TotalItens == 0) return $"Sem {SubstantivoPlural} a apresentar";

        var palavra = TotalItens == 1 ? Substantivo : SubstantivoPlural;
        return $"A mostrar {PrimeiroItem}–{UltimoItem} de {TotalItens.ToString("N0", Formatos.CulturaPt)} {palavra}";
    }
}
