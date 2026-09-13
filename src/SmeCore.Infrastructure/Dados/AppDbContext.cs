using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmeCore.Domain.Auditoria;
using SmeCore.Domain.Clientes;
using SmeCore.Domain.Veiculos;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Infrastructure.Dados;

public class AppDbContext : IdentityDbContext<Utilizador, Perfil, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ClienteContacto> ClienteContactos => Set<ClienteContacto>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<RegistoAuditoria> RegistosAuditoria => Set<RegistoAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.AplicarSnakeCase();
    }
}
