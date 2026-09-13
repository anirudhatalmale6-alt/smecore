using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;
using SmeCore.Web.Servicos;

namespace SmeCore.Web.Pages.Utilizadores;

[Authorize(Policy = Permissoes.Politicas.UtilizadoresGerirPerfis)]
public class PerfisModel : PageModel
{
    private readonly UtilizadoresServico _utilizadores;
    private readonly IPermissoesServico _permissoes;

    public PerfisModel(UtilizadoresServico utilizadores, IPermissoesServico permissoes)
    {
        _utilizadores = utilizadores;
        _permissoes = permissoes;
    }

    public List<Perfil> Perfis { get; private set; } = new();

    public Dictionary<Guid, int> Contagens { get; private set; } = new();

    /// <summary>Permissões efetivas de cada perfil, para se ver o que cada um permite sem abrir a ficha.</summary>
    public Dictionary<Guid, ISet<string>> PermissoesPorPerfil { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        Perfis = await _utilizadores.ObterPerfisAsync(ct);
        Contagens = await _utilizadores.ContarUtilizadoresPorPerfilAsync(ct);

        foreach (var perfil in Perfis)
        {
            PermissoesPorPerfil[perfil.Id] = await _permissoes.ObterPermissoesDoPerfilAsync(perfil.Name!);
        }
    }

    public async Task<IActionResult> OnPostEliminarAsync(Guid id, CancellationToken ct)
    {
        var resultado = await _utilizadores.EliminarPerfilAsync(id, ct);

        if (resultado.Sucesso) this.Sucesso("Perfil eliminado.");
        else this.Erro(resultado.Erros[0].Mensagem);

        return RedirectToPage();
    }
}
