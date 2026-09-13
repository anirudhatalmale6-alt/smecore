using System.Text;

namespace SmeCore.Infrastructure.Consultas;

public static class Pesquisa
{
    /// <summary>
    /// Transforma o texto escrito pelo utilizador num padrão ILIKE seguro. Os caracteres
    /// especiais do LIKE (%, _ e \) são escapados, senão um utilizador que escreva "50%"
    /// obtém resultados aparentemente aleatórios.
    /// </summary>
    public static string ParaPadrao(string termo)
    {
        var sb = new StringBuilder(termo.Length + 2);
        sb.Append('%');
        foreach (var c in termo.Trim())
        {
            if (c is '%' or '_' or '\\') sb.Append('\\');
            sb.Append(c);
        }
        sb.Append('%');
        return sb.ToString();
    }

    /// <summary>Texto de pesquisa limpo, ou null se não houver nada para pesquisar.</summary>
    public static string? Limpar(string? termo)
        => string.IsNullOrWhiteSpace(termo) ? null : termo.Trim();
}
