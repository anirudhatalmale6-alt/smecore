using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmeCore.Domain.Auditoria;
using SmeCore.Infrastructure.Identidade;

namespace SmeCore.Infrastructure.Dados.Configuracoes;

public class RegistoAuditoriaConfig : IEntityTypeConfiguration<RegistoAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistoAuditoria> b)
    {
        b.ToTable("registos_auditoria");

        b.Property(r => r.Entidade).HasMaxLength(100).IsRequired();
        b.Property(r => r.EntidadeId).HasMaxLength(100).IsRequired();
        b.Property(r => r.EntidadeDescricao).HasMaxLength(400);
        b.Property(r => r.UtilizadorNome).HasMaxLength(200);
        b.Property(r => r.Origem).HasMaxLength(64);
        b.Property(r => r.Acao).HasConversion<int>();

        // jsonb permite consultar o conteúdo das alterações no futuro sem migrar dados.
        b.Property(r => r.AlteracoesJson).HasColumnType("jsonb");

        b.HasIndex(r => r.Instante);
        b.HasIndex(r => new { r.Entidade, r.EntidadeId });
        b.HasIndex(r => r.UtilizadorId);
    }
}

public class UtilizadorConfig : IEntityTypeConfiguration<Utilizador>
{
    public void Configure(EntityTypeBuilder<Utilizador> b)
    {
        b.Property(u => u.NomeCompleto).HasMaxLength(200).IsRequired();
        b.Property(u => u.Cargo).HasMaxLength(120);

        b.HasIndex(u => u.Ativo);
        b.Ignore(u => u.NomeApresentacao);
        b.Ignore(u => u.Iniciais);
    }
}

public class PerfilConfig : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> b)
    {
        b.Property(p => p.Descricao).HasMaxLength(300);
    }
}
