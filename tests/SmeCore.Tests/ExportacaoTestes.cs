using System.Text;
using FluentAssertions;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Exportacao;
using Xunit;

namespace SmeCore.Tests;

public class CsvTestes
{
    private record Linha(string Nome, int? Numero, DateOnly? Data, bool Ativo, decimal Valor);

    private static TabelaExportacao<Linha> Tabela(params Linha[] linhas)
        => new(
            "Teste",
            ExportacaoBuilder.Colunas<Linha>(
                ("Nome", l => l.Nome),
                ("Número", l => l.Numero),
                ("Data", l => l.Data),
                ("Ativo", l => l.Ativo),
                ("Valor", l => l.Valor)),
            linhas);

    private static string Texto(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    [Fact]
    public void Comeca_com_bom_utf8()
    {
        // Sem o BOM, o Excel em português abre "Gasóleo" como "GasÃ³leo".
        var bytes = CsvExportador.Gerar(Tabela(new Linha("Acentuação", 1, null, true, 0m)));

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        Texto(bytes).Should().Contain("Acentuação");
    }

    [Fact]
    public void Usa_ponto_e_virgula_e_virgula_decimal()
    {
        var csv = Texto(CsvExportador.Gerar(Tabela(new Linha("Ana", 3, new DateOnly(2026, 9, 13), true, 1250.5m))));

        csv.Should().Contain("Nome;Número;Data;Ativo;Valor");
        csv.Should().Contain("Ana;3;13/09/2026;Sim;1250,50");
    }

    [Fact]
    public void Escapa_o_separador_e_as_aspas()
    {
        var csv = Texto(CsvExportador.Gerar(Tabela(new Linha("Silva; Lda. \"Norte\"", null, null, false, 0m))));

        csv.Should().Contain("\"Silva; Lda. \"\"Norte\"\"\"");
    }

    [Fact]
    public void Neutraliza_valores_que_o_excel_leria_como_formula()
    {
        var csv = Texto(CsvExportador.Gerar(Tabela(new Linha("=1+1", null, null, false, 0m))));

        csv.Should().Contain("\"'=1+1\"");
    }

    [Fact]
    public void Uma_quebra_de_linha_no_valor_nao_parte_a_linha_do_csv()
    {
        var csv = Texto(CsvExportador.Gerar(Tabela(new Linha("Rua A\nLote 3", null, null, false, 0m))));

        csv.Should().Contain("\"Rua A\nLote 3\"");
        // Cabeçalho + uma linha de dados
        csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(2);
    }

    [Fact]
    public void Enum_sai_com_o_nome_apresentavel()
        => CsvExportador.Formatar(TipoCombustivel.HibridoPlugIn).Should().Be("Híbrido plug-in");
}

public class XlsxTestes
{
    private record Linha(string Nome, DateOnly Data, int Km);

    [Fact]
    public void Gera_um_ficheiro_xlsx_valido()
    {
        var tabela = new TabelaExportacao<Linha>(
            "Veículos",
            ExportacaoBuilder.Colunas<Linha>(
                ("Nome", l => l.Nome),
                ("Data", l => l.Data),
                ("Km", l => l.Km)),
            new[] { new Linha("Ana", new DateOnly(2026, 9, 13), 125400) })
        {
            Subtitulo = "Exportado no teste"
        };

        var bytes = XlsxExportador.Gerar(tabela);

        // Um .xlsx é um zip: começa por "PK".
        bytes.Take(2).Should().Equal((byte)'P', (byte)'K');
        bytes.Length.Should().BeGreaterThan(2000);
    }

    [Fact]
    public void Nome_de_folha_comprido_e_cortado_para_o_limite_do_excel()
    {
        var nomeLongo = new string('A', 60);
        var tabela = new TabelaExportacao<Linha>(
            nomeLongo,
            ExportacaoBuilder.Colunas<Linha>(("Nome", l => l.Nome)),
            Array.Empty<Linha>());

        // Se o nome não fosse cortado, o ClosedXML lançava exceção ao criar a folha.
        var acao = () => XlsxExportador.Gerar(tabela);
        acao.Should().NotThrow();
    }
}
