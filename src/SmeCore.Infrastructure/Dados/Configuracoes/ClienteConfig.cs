using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmeCore.Domain.Clientes;

namespace SmeCore.Infrastructure.Dados.Configuracoes;

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");

        b.Property(c => c.Numero)
            .ValueGeneratedOnAdd()
            .UseIdentityAlwaysColumn();

        b.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        b.Property(c => c.Nif).HasMaxLength(20);
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Telefone).HasMaxLength(30);
        b.Property(c => c.Telemovel).HasMaxLength(30);
        b.Property(c => c.Morada).HasMaxLength(300);
        b.Property(c => c.CodigoPostal).HasMaxLength(12);
        b.Property(c => c.Localidade).HasMaxLength(120);
        b.Property(c => c.Pais).HasMaxLength(80).IsRequired();
        b.Property(c => c.Observacoes).HasMaxLength(4000);
        b.Property(c => c.CriadoPorNome).HasMaxLength(200);
        b.Property(c => c.AlteradoPorNome).HasMaxLength(200);
        b.Property(c => c.EliminadoPorNome).HasMaxLength(200);

        b.Property(c => c.Tipo).HasConversion<int>();

        b.HasIndex(c => c.Numero).IsUnique();
        b.HasIndex(c => c.Nome);
        b.HasIndex(c => c.Eliminado);

        // O NIF é único entre os clientes não eliminados. Índice filtrado, para que um
        // cliente eliminado não impeça o registo do mesmo NIF mais tarde.
        b.HasIndex(c => c.Nif)
            .IsUnique()
            .HasFilter("nif IS NOT NULL AND eliminado = false")
            .HasDatabaseName("ix_clientes_nif_unico");

        b.HasMany(c => c.Contactos)
            .WithOne(ct => ct.Cliente!)
            .HasForeignKey(ct => ct.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(c => c.Veiculos)
            .WithOne(v => v.Cliente!)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Ignore(c => c.Designacao);
    }
}

public class ClienteContactoConfig : IEntityTypeConfiguration<ClienteContacto>
{
    public void Configure(EntityTypeBuilder<ClienteContacto> b)
    {
        b.ToTable("cliente_contactos");

        b.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        b.Property(c => c.Funcao).HasMaxLength(120);
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Telefone).HasMaxLength(30);
        b.Property(c => c.CriadoPorNome).HasMaxLength(200);
        b.Property(c => c.AlteradoPorNome).HasMaxLength(200);
        b.Property(c => c.EliminadoPorNome).HasMaxLength(200);

        b.HasIndex(c => c.ClienteId);
    }
}
