namespace SmeCore.Infrastructure.Exportacao;

/// <summary>Uma coluna de exportação: título visível e como obter o valor de cada linha.</summary>
public class ColunaExportacao<T>
{
    public string Titulo { get; }
    public Func<T, object?> Valor { get; }

    public ColunaExportacao(string titulo, Func<T, object?> valor)
    {
        Titulo = titulo;
        Valor = valor;
    }
}

/// <summary>
/// Definição de uma exportação, partilhada pelo CSV e pelo Excel: as colunas são declaradas
/// uma única vez e os dois formatos saem iguais.
/// </summary>
public class TabelaExportacao<T>
{
    /// <summary>Nome do ficheiro sem extensão e nome da folha no Excel.</summary>
    public string Nome { get; }

    public IReadOnlyList<ColunaExportacao<T>> Colunas { get; }

    public IReadOnlyList<T> Linhas { get; }

    /// <summary>Linha de contexto escrita acima da tabela (filtros aplicados, data da extração).</summary>
    public string? Subtitulo { get; init; }

    public TabelaExportacao(string nome, IReadOnlyList<ColunaExportacao<T>> colunas, IReadOnlyList<T> linhas)
    {
        Nome = nome;
        Colunas = colunas;
        Linhas = linhas;
    }
}

public static class ExportacaoBuilder
{
    public static List<ColunaExportacao<T>> Colunas<T>(params (string Titulo, Func<T, object?> Valor)[] definicoes)
        => definicoes.Select(d => new ColunaExportacao<T>(d.Titulo, d.Valor)).ToList();
}
