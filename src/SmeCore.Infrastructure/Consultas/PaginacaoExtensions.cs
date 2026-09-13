using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Common;

namespace SmeCore.Infrastructure.Consultas;

public static class PaginacaoExtensions
{
    /// <summary>
    /// Conta o total e devolve apenas a página pedida. Se a página pedida já não existir
    /// (por exemplo depois de aplicar um filtro), recua para a última página com resultados
    /// em vez de mostrar uma tabela vazia.
    /// </summary>
    public static async Task<ResultadoPaginado<T>> ParaPaginaAsync<T>(
        this IQueryable<T> query,
        ParametrosListagem parametros,
        CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);

        var totalPaginas = total == 0 ? 1 : (int)Math.Ceiling(total / (double)parametros.TamanhoPagina);
        var pagina = Math.Min(parametros.Pagina, totalPaginas);

        var itens = await query
            .Skip((pagina - 1) * parametros.TamanhoPagina)
            .Take(parametros.TamanhoPagina)
            .ToListAsync(ct);

        return new ResultadoPaginado<T>(itens, total, pagina, parametros.TamanhoPagina);
    }

    /// <summary>
    /// Limite de segurança para exportações: impede que um filtro largo tente materializar
    /// a tabela inteira em memória.
    /// </summary>
    public const int LimiteExportacao = 20_000;

    public static async Task<(List<T> Itens, bool Truncado)> ParaExportacaoAsync<T>(
        this IQueryable<T> query,
        CancellationToken ct = default)
    {
        // Pede-se um registo a mais do que o limite: se vier, sabemos que houve corte.
        var itens = await query.Take(LimiteExportacao + 1).ToListAsync(ct);

        if (itens.Count > LimiteExportacao)
        {
            itens.RemoveRange(LimiteExportacao, itens.Count - LimiteExportacao);
            return (itens, true);
        }

        return (itens, false);
    }
}
