using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmeCore.Infrastructure.Identidade;

/// <summary>
/// SignInManager que recusa contas desativadas. Fica aqui, e não na página de login, para que
/// a regra se aplique também à revalidação periódica do cookie: desativar um utilizador
/// expulsa-o da aplicação sem ser preciso esperar que ele saia.
/// </summary>
public class GestorSessao : SignInManager<Utilizador>
{
    public GestorSessao(
        UserManager<Utilizador> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<Utilizador> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<Utilizador>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<Utilizador> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(Utilizador user)
    {
        if (!user.Ativo)
        {
            Logger.LogInformation("Sessão recusada: a conta {Id} está desativada.", user.Id);
            return false;
        }

        return await base.CanSignInAsync(user);
    }
}
