using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Common;
using SmeCore.Infrastructure;
using SmeCore.Infrastructure.Dados;
using SmeCore.Infrastructure.Identidade;
using SmeCore.Web.Servicos;

var builder = WebApplication.CreateBuilder(args);

// Toda a aplicação corre em pt-PT: datas dd/MM/aaaa, vírgula decimal e ordenação de texto
// com acentos. É definido antes de tudo o resto para que também as mensagens de validação
// geradas pelo framework saiam nesta cultura.
var culturaPt = CultureInfo.GetCultureInfo("pt-PT");
CultureInfo.DefaultThreadCurrentCulture = culturaPt;
CultureInfo.DefaultThreadCurrentUICulture = culturaPt;

builder.Services.AdicionarInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilizadorAtual, UtilizadorAtualHttp>();

builder.Services.AddRazorPages(opcoes =>
{
    // Tudo exige sessão iniciada, menos o que for explicitamente marcado como público.
    opcoes.Conventions.AuthorizeFolder("/");
    opcoes.Conventions.AllowAnonymousToFolder("/Conta");
    opcoes.Conventions.AllowAnonymousToPage("/Erro");

    // Os formulários de criação e de edição são a mesma página, servida em dois endereços.
    // Evita duplicar o modelo e a vista só para mudar o título.
    opcoes.Conventions.AddPageRoute("/Clientes/Editar", "/clientes/novo");
    opcoes.Conventions.AddPageRoute("/Veiculos/Editar", "/veiculos/novo");
    opcoes.Conventions.AddPageRoute("/Utilizadores/Editar", "/utilizadores/novo");
    opcoes.Conventions.AddPageRoute("/Utilizadores/EditarPerfil", "/perfis/novo");
});

builder.Services.AddRequestLocalization(opcoes =>
{
    opcoes.DefaultRequestCulture = new RequestCulture(culturaPt);
    opcoes.SupportedCultures = new List<CultureInfo> { culturaPt };
    opcoes.SupportedUICultures = new List<CultureInfo> { culturaPt };
});

builder.Services.ConfigureApplicationCookie(opcoes =>
{
    opcoes.LoginPath = "/entrar";
    opcoes.LogoutPath = "/sair";
    opcoes.AccessDeniedPath = "/acesso-negado";
    opcoes.ReturnUrlParameter = "regressar";
    opcoes.ExpireTimeSpan = TimeSpan.FromHours(10);
    opcoes.SlidingExpiration = true;
    opcoes.Cookie.Name = "smecore.sessao";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SameSite = SameSiteMode.Lax;

    // Em produção o cookie só viaja em HTTPS. Em desenvolvimento aceita-se HTTP para
    // não obrigar a certificados locais.
    opcoes.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Revalidar o cookie a cada 5 minutos: é este intervalo que faz com que desativar uma conta
// (ou mudar-lhe os perfis) tenha efeito sem esperar que a sessão expire.
builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(opcoes =>
{
    opcoes.ValidationInterval = TimeSpan.FromMinutes(5);
});

builder.Services.Configure<ForwardedHeadersOptions>(opcoes =>
{
    // No App Platform a aplicação está atrás do balanceador da DigitalOcean: sem isto, os
    // redirecionamentos saem em http e o IP registado no histórico é o do balanceador.
    opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    opcoes.KnownNetworks.Clear();
    opcoes.KnownProxies.Clear();
});

builder.Services.Configure<RouteOptions>(opcoes =>
{
    opcoes.LowercaseUrls = true;
    opcoes.LowercaseQueryStrings = false;
});

builder.Services.AddAntiforgery(opcoes => opcoes.HeaderName = "RequestVerificationToken");

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("base-de-dados");

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/erro");
    app.UseStatusCodePagesWithReExecute("/erro/{0}");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// O healthcheck responde com o nome da aplicação, para não haver dúvidas sobre qual o serviço
// que respondeu quando várias aplicações partilham o mesmo domínio ou porta.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (contexto, relatorio) =>
    {
        contexto.Response.ContentType = "application/json; charset=utf-8";
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            aplicacao = "SmeCore",
            estado = relatorio.Status.ToString(),
            verificacoes = relatorio.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())
        });
        await contexto.Response.WriteAsync(payload);
    }
}).AllowAnonymous();

await AplicarMigracoesESemearAsync(app);

app.Run();

// Aplica as migrações pendentes e prepara perfis/conta inicial. Corre no arranque para que um
// deploy no App Platform não precise de nenhum passo manual; pode ser desligado com
// BaseDados:MigrarNoArranque=false quando as migrações são aplicadas por outro processo.
static async Task AplicarMigracoesESemearAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("BaseDados:MigrarNoArranque", true))
    {
        app.Logger.LogInformation("Migração automática desligada por configuração.");
        return;
    }

    using var escopo = app.Services.CreateScope();
    var log = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Arranque");

    try
    {
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        var pendentes = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (pendentes.Count > 0)
        {
            log.LogInformation("A aplicar {Total} migração(ões): {Nomes}", pendentes.Count, string.Join(", ", pendentes));
            await db.Database.MigrateAsync();
        }

        var semeador = escopo.ServiceProvider.GetRequiredService<SemeadorBaseDados>();
        await semeador.ExecutarAsync();
    }
    catch (Exception ex)
    {
        // Falhar o arranque é melhor do que servir uma aplicação com o esquema errado.
        log.LogCritical(ex, "Falha ao preparar a base de dados no arranque.");
        throw;
    }
}
