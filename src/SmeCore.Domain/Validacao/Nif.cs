using System.Text;

namespace SmeCore.Domain.Validacao;

/// <summary>
/// Validação e normalização do NIF/NIPC português (9 dígitos com dígito de controlo módulo 11).
/// </summary>
public static class Nif
{
    /// <summary>
    /// Primeiros dígitos válidos. Um dígito isolado cobre as séries de 1 dígito;
    /// os pares cobrem as séries que só são válidas com dois dígitos (ex.: 45, 70, 98).
    /// </summary>
    private static readonly string[] PrefixosValidos =
    {
        "1", "2", "3",           // pessoas singulares
        "45",                    // pessoas singulares não residentes
        "5",                     // pessoas coletivas
        "6",                     // organismos da administração pública
        "70", "74", "75",        // heranças indivisas / entidades equiparadas
        "71",                    // pessoas coletivas não residentes
        "72",                    // fundos de investimento
        "77",                    // atribuição oficiosa
        "78",                    // não residentes (atribuição oficiosa)
        "79",                    // regime excecional
        "8",                     // empresário em nome individual (extinto, mas ainda em circulação)
        "90", "91",              // condomínios, sociedades irregulares
        "98",                    // não residentes sem estabelecimento estável
        "99"                     // sociedades civis
    };

    /// <summary>
    /// Remove tudo o que não seja dígito. Não usa uma lista de separadores permitidos:
    /// qualquer caractere que não seja um dígito é descartado.
    /// </summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var sb = new StringBuilder(valor.Length);
        foreach (var c in valor)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Verdadeiro quando o valor é um NIF português válido (prefixo e dígito de controlo).</summary>
    public static bool EhValido(string? valor)
    {
        var digitos = Normalizar(valor);
        if (digitos.Length != 9) return false;

        if (!TemPrefixoValido(digitos)) return false;

        var soma = 0;
        for (var i = 0; i < 8; i++)
        {
            soma += (digitos[i] - '0') * (9 - i);
        }

        var resto = soma % 11;
        var controlo = resto < 2 ? 0 : 11 - resto;

        return controlo == digitos[8] - '0';
    }

    private static bool TemPrefixoValido(string digitos)
    {
        foreach (var prefixo in PrefixosValidos)
        {
            // As séries de 1 dígito (1, 2, 3, 5, 6, 8) e as de 2 dígitos (45, 7x, 9x) não se
            // sobrepõem, pelo que basta encontrar o primeiro prefixo que corresponda.
            if (digitos.StartsWith(prefixo, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    /// <summary>Formata como "123 456 789" para leitura; devolve o valor original se não for válido.</summary>
    public static string Formatar(string? valor)
    {
        var digitos = Normalizar(valor);
        if (digitos.Length != 9) return valor?.Trim() ?? string.Empty;
        return $"{digitos[..3]} {digitos[3..6]} {digitos[6..]}";
    }
}
