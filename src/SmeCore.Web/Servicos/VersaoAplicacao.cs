using System.Reflection;

namespace SmeCore.Web.Servicos;

/// <summary>
/// Versão apresentada no rodapé. Vem do assembly, definido no .csproj, para que a versão
/// visível na aplicação seja sempre a que foi efetivamente publicada.
/// </summary>
public static class VersaoAplicacao
{
    public static string Numero { get; } = Resolver();

    private static string Resolver()
    {
        var informacional = typeof(VersaoAplicacao).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informacional)) return "1.0.0";

        // O SDK acrescenta "+<hash do commit>"; no rodapé só interessa a versão.
        var mais = informacional.IndexOf('+');
        return mais > 0 ? informacional[..mais] : informacional;
    }
}
