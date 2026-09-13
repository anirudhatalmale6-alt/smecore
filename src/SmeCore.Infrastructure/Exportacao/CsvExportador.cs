using System.Text;
using SmeCore.Domain.Common;

namespace SmeCore.Infrastructure.Exportacao;

/// <summary>
/// CSV pensado para abrir directamente num Excel português: separador ponto e vírgula,
/// vírgula decimal, datas dd/MM/aaaa e BOM UTF-8 (sem o BOM, o Excel estraga os acentos).
/// </summary>
public static class CsvExportador
{
    public const string TipoConteudo = "text/csv";
    private const char Separador = ';';

    public static byte[] Gerar<T>(TabelaExportacao<T> tabela)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(tabela.Subtitulo))
        {
            sb.Append(Escapar(tabela.Subtitulo)).Append('\r').Append('\n');
            sb.Append('\r').Append('\n');
        }

        for (var i = 0; i < tabela.Colunas.Count; i++)
        {
            if (i > 0) sb.Append(Separador);
            sb.Append(Escapar(tabela.Colunas[i].Titulo));
        }
        sb.Append('\r').Append('\n');

        foreach (var linha in tabela.Linhas)
        {
            for (var i = 0; i < tabela.Colunas.Count; i++)
            {
                if (i > 0) sb.Append(Separador);
                sb.Append(Escapar(Formatar(tabela.Colunas[i].Valor(linha))));
            }
            sb.Append('\r').Append('\n');
        }

        // Encoding.UTF8 escreve o BOM; é ele que faz o Excel reconhecer os acentos.
        return Encoding.UTF8.GetPreamble()
            .Concat(new UTF8Encoding(false).GetBytes(sb.ToString()))
            .ToArray();
    }

    internal static string Formatar(object? valor) => valor switch
    {
        null => string.Empty,
        string s => s,
        bool b => b ? "Sim" : "Não",
        DateOnly d => d.ToString("dd/MM/yyyy", Formatos.CulturaPt),
        DateTime d => d.ToString("dd/MM/yyyy HH:mm", Formatos.CulturaPt),
        DateTimeOffset d => d.ToString("dd/MM/yyyy HH:mm", Formatos.CulturaPt),
        decimal m => m.ToString("0.00", Formatos.CulturaPt),
        double d => d.ToString("0.00", Formatos.CulturaPt),
        Enum e => Formatos.Enumeracao(e),
        _ => Convert.ToString(valor, Formatos.CulturaPt) ?? string.Empty
    };

    /// <summary>
    /// Envolve em aspas sempre que o valor contém separador, aspas ou fim de linha, e duplica
    /// as aspas interiores. Valores que comecem por =, + ou @ levam um apóstrofo à frente,
    /// para o Excel os tratar como texto e não como fórmula.
    /// </summary>
    internal static string Escapar(string valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        var perigoso = valor[0] is '=' or '+' or '@';
        var precisaAspas = perigoso
            || valor.Contains(Separador)
            || valor.Contains('"')
            || valor.Contains('\n')
            || valor.Contains('\r');

        if (!precisaAspas) return valor;

        var interior = valor.Replace("\"", "\"\"");
        if (perigoso) interior = "'" + interior;

        return $"\"{interior}\"";
    }
}
