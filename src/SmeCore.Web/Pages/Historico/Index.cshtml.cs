using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;

namespace SmeCore.Web.Pages.Historico;

[Authorize(Policy = Permissoes.Politicas.HistoricoVer)]
public class IndexModel : PageModel
{
    private readonly HistoricoServico _historico;
    private readonly UtilizadoresServico _utilizadores;

    public IndexModel(HistoricoServico historico, UtilizadoresServico utilizadores)
    {
        _historico = historico;
        _utilizadores = utilizadores;
    }

    [BindProperty(SupportsGet = true, Name = "")]
    public HistoricoFiltro Filtro { get; set; } = new();

    public ResultadoPaginado<HistoricoLinha> Pagina { get; private set; } = ResultadoPaginado<HistoricoLinha>.Vazio();

    public List<string> Entidades { get; private set; } = new();
    public List<UtilizadorListagem> Utilizadores { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        // O histórico é sempre apresentado do mais recente para o mais antigo, salvo indicação
        // em contrário do utilizador.
        Filtro.Ordenar ??= "instante";

        Pagina = await _historico.ObterPaginaAsync(Filtro, ct);
        Entidades = await _historico.ObterEntidadesAsync(ct);

        var utilizadores = await _utilizadores.ObterPaginaAsync(
            new UtilizadoresFiltro { TamanhoPagina = 200, Ordenar = "nome" }, ct);
        Utilizadores = utilizadores.Itens.ToList();
    }

    public static IEnumerable<AcaoAuditoria> Acoes => Enum.GetValues<AcaoAuditoria>();
}
