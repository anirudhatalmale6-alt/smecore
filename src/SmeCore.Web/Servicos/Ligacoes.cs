namespace SmeCore.Web.Servicos;

/// <summary>
/// Constrói os parâmetros de um link de listagem preservando os filtros que já estão na barra
/// de endereço. Sem isto, clicar num cabeçalho para ordenar apagaria a pesquisa em curso.
/// </summary>
public static class Ligacoes
{
    public static Dictionary<string, string?> Com(
        HttpRequest pedido,
        params (string Chave, string? Valor)[] alteracoes)
    {
        var dados = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var par in pedido.Query)
        {
            var valor = par.Value.ToString();
            if (!string.IsNullOrEmpty(valor)) dados[par.Key] = valor;
        }

        foreach (var (chave, valor) in alteracoes)
        {
            // Valor nulo remove o parâmetro — é assim que se volta à página 1 ao mudar um filtro.
            if (valor is null) dados.Remove(chave);
            else dados[chave] = valor;
        }

        return dados;
    }

    /// <summary>Parâmetros de exportação: os mesmos filtros, sem paginação.</summary>
    public static Dictionary<string, string?> ParaExportacao(HttpRequest pedido, string formato)
        => Com(pedido,
            ("pagina", null),
            ("tamanhoPagina", null),
            ("handler", "Exportar"),
            ("formato", formato));

    /// <summary>Parâmetros do pedido HTMX que devolve apenas a tabela.</summary>
    public static Dictionary<string, string?> SemHandler(HttpRequest pedido)
        => Com(pedido, ("handler", null));
}
