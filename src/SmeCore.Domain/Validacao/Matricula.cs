using System.Text;

namespace SmeCore.Domain.Validacao;

/// <summary>
/// Normalização e validação de matrículas portuguesas nos quatro formatos em circulação.
/// </summary>
public static class Matricula
{
    /// <summary>
    /// Formatos aceites, do mais antigo ao mais recente:
    /// LL-NN-NN (até 1992), NN-NN-LL (1992-2005), NN-LL-NN (2005-2020), LL-NN-LL (desde 03/2020).
    /// </summary>
    private static readonly string[] Padroes = { "LLNN", "NNLL", "NLLN", "LNNL" };

    /// <summary>
    /// Deixa apenas letras e dígitos, em maiúsculas. Não há lista de separadores permitidos:
    /// espaços, pontos, hífenes de qualquer tipo e qualquer outro caractere são removidos.
    /// </summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var sb = new StringBuilder(valor.Length);
        foreach (var c in valor)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Verdadeiro se corresponder a um dos formatos nacionais (6 caracteres alfanuméricos).</summary>
    public static bool EhValida(string? valor)
    {
        var limpa = Normalizar(valor);
        if (limpa.Length != 6) return false;

        // Cada padrão descreve os três pares: L = par de letras, N = par de dígitos.
        foreach (var padrao in Padroes)
        {
            if (Corresponde(limpa, padrao)) return true;
        }
        return false;
    }

    private static bool Corresponde(string limpa, string padrao)
    {
        return padrao switch
        {
            "LLNN" => Letras(limpa, 0, 2) && Digitos(limpa, 2, 4),   // LL-NN-NN
            "NNLL" => Digitos(limpa, 0, 4) && Letras(limpa, 4, 2),   // NN-NN-LL
            "NLLN" => Digitos(limpa, 0, 2) && Letras(limpa, 2, 2) && Digitos(limpa, 4, 2), // NN-LL-NN
            "LNNL" => Letras(limpa, 0, 2) && Digitos(limpa, 2, 2) && Letras(limpa, 4, 2),  // LL-NN-LL
            _ => false
        };
    }

    private static bool Letras(string s, int inicio, int quantos)
    {
        for (var i = inicio; i < inicio + quantos; i++)
        {
            if (i >= s.Length || !char.IsAsciiLetterUpper(s[i])) return false;
        }
        return true;
    }

    private static bool Digitos(string s, int inicio, int quantos)
    {
        for (var i = inicio; i < inicio + quantos; i++)
        {
            if (i >= s.Length || !char.IsAsciiDigit(s[i])) return false;
        }
        return true;
    }

    /// <summary>Devolve a matrícula no formato AA-00-AA. Se não for válida, devolve o valor limpo.</summary>
    public static string Formatar(string? valor)
    {
        var limpa = Normalizar(valor);
        if (limpa.Length != 6) return limpa;
        return $"{limpa[..2]}-{limpa[2..4]}-{limpa[4..]}";
    }
}
