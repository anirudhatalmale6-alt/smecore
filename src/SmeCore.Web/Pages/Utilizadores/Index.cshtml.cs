using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Utilizadores;

[Authorize(Policy = Permissoes.Politicas.UtilizadoresVer)]
public class IndexModel : PageModel
{
    private readonly UtilizadoresServico _utilizadores;
    private readonly IPermissoesServico _permissoes;

    public IndexModel(UtilizadoresServico utilizadores, IPermissoesServico permissoes)
    {
        _utilizadores = utilizadores;
        _permissoes = permissoes;
    }

    [BindProperty(SupportsGet = true, Name = "")]
    public UtilizadoresFiltro Filtro { get; set; } = new();

    public ResultadoPaginado<UtilizadorListagem> Pagina { get; private set; } = ResultadoPaginado<UtilizadorListagem>.Vazio();

    public List<Perfil> Perfis { get; private set; } = new();

    public bool PodeGerir { get; private set; }

    public Guid? EuMesmo { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await CarregarAsync(ct);

    public async Task<IActionResult> OnPostEstadoAsync(Guid id, bool ativo, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Utilizadores.Gerir)) return Forbid();

        var resultado = await _utilizadores.DefinirEstadoAsync(id, ativo, LerIdDoUtilizadorAtual(), ct);

        if (resultado.Sucesso) this.Sucesso(ativo ? "Utilizador ativado." : "Utilizador desativado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    public async Task<IActionResult> OnPostDesbloquearAsync(Guid id, CancellationToken ct)
    {
        if (!await _permissoes.TemPermissaoAsync(User, Permissoes.Utilizadores.Gerir)) return Forbid();

        var resultado = await _utilizadores.DesbloquearAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Conta desbloqueada.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage(new { });
    }

    private async Task CarregarAsync(CancellationToken ct)
    {
        PodeGerir = await _permissoes.TemPermissaoAsync(User, Permissoes.Utilizadores.Gerir);
        EuMesmo = LerIdDoUtilizadorAtual();

        Pagina = await _utilizadores.ObterPaginaAsync(Filtro, ct);
        Perfis = await _utilizadores.ObterPerfisAsync(ct);
    }

    private Guid? LerIdDoUtilizadorAtual()
    {
        var valor = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(valor, out var id) ? id : null;
    }
}
