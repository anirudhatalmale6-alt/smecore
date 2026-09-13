using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SmeCore.Infrastructure.Identidade;

/// <summary>
/// Acrescenta ao cookie o nome de apresentação, para o cabeçalho e o histórico não terem de
/// ir à base de dados em cada pedido só para saber como se chama quem está autenticado.
/// </summary>
public class FabricaClaims : UserClaimsPrincipalFactory<Utilizador, Perfil>
{
    public const string ClaimNome = "nome_apresentacao";

    public FabricaClaims(
        UserManager<Utilizador> userManager,
        RoleManager<Perfil> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Utilizador user)
    {
        var identidade = await base.GenerateClaimsAsync(user);
        identidade.AddClaim(new Claim(ClaimNome, user.NomeApresentacao));
        return identidade;
    }
}
