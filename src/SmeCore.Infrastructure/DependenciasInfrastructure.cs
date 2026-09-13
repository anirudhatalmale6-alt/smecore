using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Infrastructure.Servicos;

namespace SmeCore.Infrastructure;

public static class DependenciasInfrastructure
{
    /// <summary>
    /// Liga a base de dados, o Identity, as permissões e os serviços de módulo. Um módulo novo
    /// acrescenta aqui uma linha; não há nada mais para configurar.
    /// </summary>
    public static IServiceCollection AdicionarInfrastructure(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        var ligacao = ResolverStringLigacao(configuracao);

        servicos.AddSingleton<IRelogio, RelogioSistema>();
        servicos.AddScoped<AuditoriaInterceptor>();

        servicos.AddDbContext<AppDbContext>((sp, opcoes) =>
        {
            opcoes.UseNpgsql(ligacao, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                // Uma falha de rede momentânea no App Platform não deve resultar em erro 500.
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
            });

            opcoes.AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>());
        });

        servicos
            .AddIdentity<Utilizador, Perfil>(opcoes =>
            {
                opcoes.Password.RequiredLength = 10;
                opcoes.Password.RequireDigit = true;
                opcoes.Password.RequireUppercase = true;
                opcoes.Password.RequireLowercase = true;
                opcoes.Password.RequireNonAlphanumeric = false;

                opcoes.User.RequireUniqueEmail = true;
                opcoes.SignIn.RequireConfirmedAccount = false;

                opcoes.Lockout.MaxFailedAccessAttempts = 5;
                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<GestorSessao>()
            .AddClaimsPrincipalFactory<FabricaClaims>()
            .AddDefaultTokenProviders();

        servicos.AddMemoryCache();
        servicos.AddScoped<IPermissoesServico, PermissoesServico>();
        servicos.AddSingleton<IAuthorizationPolicyProvider, PermissaoPolicyProvider>();
        servicos.AddScoped<IAuthorizationHandler, PermissaoHandler>();

        servicos.AddScoped<ClientesServico>();
        servicos.AddScoped<VeiculosServico>();
        servicos.AddScoped<HistoricoServico>();
        servicos.AddScoped<UtilizadoresServico>();
        servicos.AddScoped<SemeadorBaseDados>();

        return servicos;
    }

    /// <summary>
    /// Aceita a ligação em três formatos: ConnectionStrings:DefaultConnection, a variável
    /// DATABASE_URL no formato postgres:// (é o que a DigitalOcean injeta) ou as variáveis
    /// PG* individuais. Sem isto, cada ambiente exigia um ficheiro de configuração diferente.
    /// </summary>
    public static string ResolverStringLigacao(IConfiguration configuracao)
    {
        var directa = configuracao.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(directa)) return directa;

        var url = configuracao["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(url)) return ConverterUrlPostgres(url);

        throw new InvalidOperationException(
            "Falta a ligação à base de dados. Defina ConnectionStrings__DefaultConnection ou DATABASE_URL.");
    }

    /// <summary>Converte postgres://utilizador:senha@servidor:porta/base?sslmode=require em keyword/value do Npgsql.</summary>
    public static string ConverterUrlPostgres(string url)
    {
        var uri = new Uri(url);
        var credenciais = uri.UserInfo.Split(':', 2);

        var baseDados = uri.AbsolutePath.TrimStart('/');
        var porta = uri.Port > 0 ? uri.Port : 5432;

        // No Npgsql 8, "Require" cifra a ligação sem validar a cadeia de certificação e
        // "VerifyFull" valida-a — é por isso que não há aqui nenhum TrustServerCertificate.
        var sslMode = "Require";

        var consulta = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (consulta.TryGetValue("sslmode", out var valorSsl) && !string.IsNullOrWhiteSpace(valorSsl))
        {
            sslMode = valorSsl.ToString() switch
            {
                "disable" => "Disable",
                "allow" => "Prefer",
                "prefer" => "Prefer",
                "require" => "Require",
                "verify-ca" => "VerifyCA",
                "verify-full" => "VerifyFull",
                _ => "Require"
            };
        }

        var senha = credenciais.Length > 1 ? Uri.UnescapeDataString(credenciais[1]) : string.Empty;

        var construtor = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = porta,
            Username = Uri.UnescapeDataString(credenciais[0]),
            Password = senha,
            Database = baseDados,
            SslMode = Enum.Parse<Npgsql.SslMode>(sslMode, ignoreCase: true)
        };

        return construtor.ConnectionString;
    }
}
