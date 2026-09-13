using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Web.Pages.Conta;

public class SairModel : PageModel
{
    private readonly SignInManager<Utilizador> _sessoes;

    public SairModel(SignInManager<Utilizador> sessoes) => _sessoes = sessoes;

    /// <summary>
    /// O fecho de sessão acontece por POST com token anti-falsificação: um GET permitiria que
    /// uma imagem ou um link num e-mail terminasse a sessão de quem o abrisse.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await _sessoes.SignOutAsync();
        return Page();
    }

    public IActionResult OnGet()
    {
        // Um GET em /sair não fecha nada: quem ainda tem sessão volta ao painel e quem não
        // tem vai para o formulário de entrada.
        return User.Identity?.IsAuthenticated == true ? LocalRedirect("/") : LocalRedirect("/entrar");
    }
}
