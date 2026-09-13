using ClosedXML.Excel;
using SmeCore.Domain.Common;

namespace SmeCore.Infrastructure.Exportacao;

/// <summary>
/// Excel real (.xlsx), com cabeçalho fixo, filtro automático e tipos nativos — datas como
/// datas e números como números, para o utilizador poder ordenar e somar sem converter nada.
/// </summary>
public static class XlsxExportador
{
    public const string TipoConteudo = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static byte[] Gerar<T>(TabelaExportacao<T> tabela)
    {
        using var livro = new XLWorkbook();
        livro.Style.Font.FontName = "Calibri";

        var folha = livro.Worksheets.Add(NomeFolhaValido(tabela.Nome));

        var linhaAtual = 1;

        if (!string.IsNullOrWhiteSpace(tabela.Subtitulo))
        {
            var celula = folha.Cell(linhaAtual, 1);
            celula.Value = tabela.Subtitulo;
            celula.Style.Font.Italic = true;
            celula.Style.Font.FontColor = XLColor.FromArgb(90, 98, 112);
            folha.Range(linhaAtual, 1, linhaAtual, Math.Max(1, tabela.Colunas.Count)).Merge();
            linhaAtual += 2;
        }

        var linhaCabecalho = linhaAtual;
        for (var c = 0; c < tabela.Colunas.Count; c++)
        {
            var celula = folha.Cell(linhaCabecalho, c + 1);
            celula.Value = tabela.Colunas[c].Titulo;
            celula.Style.Font.Bold = true;
            celula.Style.Font.FontColor = XLColor.White;
            celula.Style.Fill.BackgroundColor = XLColor.FromArgb(31, 58, 95);
            celula.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        folha.Row(linhaCabecalho).Height = 20;

        var linha = linhaCabecalho + 1;
        foreach (var item in tabela.Linhas)
        {
            for (var c = 0; c < tabela.Colunas.Count; c++)
            {
                Escrever(folha.Cell(linha, c + 1), tabela.Colunas[c].Valor(item));
            }
            linha++;
        }

        var ultimaLinha = Math.Max(linhaCabecalho, linha - 1);
        var intervalo = folha.Range(linhaCabecalho, 1, ultimaLinha, Math.Max(1, tabela.Colunas.Count));
        intervalo.SetAutoFilter();
        folha.SheetView.FreezeRows(linhaCabecalho);
        folha.Columns().AdjustToContents(10d, 55d);

        using var memoria = new MemoryStream();
        livro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void Escrever(IXLCell celula, object? valor)
    {
        switch (valor)
        {
            case null:
                break;
            case string s:
                celula.SetValue(s);
                break;
            case bool b:
                celula.SetValue(b ? "Sim" : "Não");
                break;
            case DateOnly d:
                celula.SetValue(d.ToDateTime(TimeOnly.MinValue));
                celula.Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            case DateTime dt:
                celula.SetValue(dt);
                celula.Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
                break;
            case DateTimeOffset dto:
                celula.SetValue(dto.DateTime);
                celula.Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
                break;
            case int i:
                celula.SetValue(i);
                celula.Style.NumberFormat.Format = "#,##0";
                break;
            case long l:
                celula.SetValue(l);
                celula.Style.NumberFormat.Format = "#,##0";
                break;
            case decimal m:
                celula.SetValue(m);
                celula.Style.NumberFormat.Format = "#,##0.00";
                break;
            case double db:
                celula.SetValue(db);
                celula.Style.NumberFormat.Format = "#,##0.00";
                break;
            case Enum e:
                celula.SetValue(Formatos.Enumeracao(e));
                break;
            default:
                celula.SetValue(Convert.ToString(valor, Formatos.CulturaPt));
                break;
        }
    }

    /// <summary>O Excel rejeita nomes de folha com mais de 31 caracteres ou com : \ / ? * [ ].</summary>
    private static string NomeFolhaValido(string nome)
    {
        var limpo = new string(nome.Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(limpo)) limpo = "Dados";
        return limpo.Length > 31 ? limpo[..31] : limpo;
    }
}
