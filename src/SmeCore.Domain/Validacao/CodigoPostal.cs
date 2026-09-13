using System.Text;

namespace SmeCore.Domain.Validacao;

/// <summary>Código postal português: 4 dígitos + 3 dígitos (ex.: 4700-123).</summary>
public static class CodigoPostal
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

        var sb = new StringBuilder(7);
        foreach (var c in valor)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }

    public static bool EhValido(string? valor) => Normalizar(valor).Length == 7;

    public static string Formatar(string? valor)
    {
        var digitos = Normalizar(valor);
        return digitos.Length == 7 ? $"{digitos[..4]}-{digitos[4..]}" : (valor?.Trim() ?? string.Empty);
    }
}
