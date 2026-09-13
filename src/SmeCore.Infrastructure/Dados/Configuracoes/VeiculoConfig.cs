using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmeCore.Domain.Veiculos;

namespace SmeCore.Infrastructure.Dados.Configuracoes;

public class VeiculoConfig : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> b)
    {
        b.ToTable("veiculos");

        b.Property(v => v.Matricula).HasMaxLength(20).IsRequired();
        b.Property(v => v.Vin).HasMaxLength(20);
        b.Property(v => v.Marca).HasMaxLength(80).IsRequired();
        b.Property(v => v.Modelo).HasMaxLength(120).IsRequired();
        b.Property(v => v.Versao).HasMaxLength(120);
        b.Property(v => v.Cor).HasMaxLength(60);
        b.Property(v => v.Observacoes).HasMaxLength(4000);
        b.Property(v => v.CriadoPorNome).HasMaxLength(200);
        b.Property(v => v.AlteradoPorNome).HasMaxLength(200);
        b.Property(v => v.EliminadoPorNome).HasMaxLength(200);

        b.Property(v => v.Combustivel).HasConversion<int>();
        b.Property(v => v.Caixa).HasConversion<int>();

        b.HasIndex(v => v.ClienteId);
        b.HasIndex(v => v.Marca);
        b.HasIndex(v => v.ProximaInspecao);
        b.HasIndex(v => v.Eliminado);

        // Duas viaturas ativas não podem partilhar matrícula, mas uma viatura eliminada
        // não deve bloquear o registo da mesma matrícula por um novo proprietário.
        b.HasIndex(v => v.Matricula)
            .IsUnique()
            .HasFilter("eliminado = false")
            .HasDatabaseName("ix_veiculos_matricula_unica");

        b.HasIndex(v => v.Vin)
            .IsUnique()
            .HasFilter("vin IS NOT NULL AND eliminado = false")
            .HasDatabaseName("ix_veiculos_vin_unico");

        b.Ignore(v => v.Designacao);
    }
}
