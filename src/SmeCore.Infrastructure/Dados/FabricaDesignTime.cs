using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmeCore.Infrastructure.Dados;

/// <summary>
/// Usada apenas pelas ferramentas (dotnet ef migrations / database update). Sem ela, o
/// "dotnet ef" tentaria arrancar a aplicação inteira só para descobrir o DbContext, o que
/// aplicaria migrações e seed sem ninguém ter pedido.
///
/// A ligação vem da variável de ambiente ConnectionStrings__DefaultConnection ou DATABASE_URL.
/// Não há aqui nenhuma credencial escrita no código.
/// </summary>
public class FabricaDesignTime : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var ligacao =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? ConverterSeUrl(Environment.GetEnvironmentVariable("DATABASE_URL"))
            // Valor usado apenas para gerar ficheiros de migração, quando não é preciso
            // contactar nenhuma base de dados.
            ?? "Host=localhost;Port=5432;Database=smecore;Username=postgres;Password=postgres";

        var opcoes = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ligacao, npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(opcoes);
    }

    private static string? ConverterSeUrl(string? url)
        => string.IsNullOrWhiteSpace(url) ? null : DependenciasInfrastructure.ConverterUrlPostgres(url);
}
