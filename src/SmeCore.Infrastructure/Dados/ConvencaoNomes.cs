using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SmeCore.Infrastructure.Dados;

/// <summary>
/// Converte tabelas e colunas para snake_case, a convenção normal em PostgreSQL. Evita ter de
/// escrever aspas em cada consulta SQL feita à mão e mantém o esquema legível no psql.
/// </summary>
public static class ConvencaoNomes
{
    public static void AplicarSnakeCase(this ModelBuilder modelBuilder)
    {
        foreach (var entidade in modelBuilder.Model.GetEntityTypes())
        {
            var nomeTabela = entidade.GetTableName();
            if (nomeTabela is not null)
            {
                entidade.SetTableName(ParaSnakeCase(nomeTabela));
            }

            var identificador = StoreObjectIdentifier.Table(
                ParaSnakeCase(nomeTabela ?? entidade.GetDefaultTableName() ?? entidade.ClrType.Name),
                entidade.GetSchema());

            foreach (var propriedade in entidade.GetProperties())
            {
                var nomeColuna = propriedade.GetColumnName(identificador) ?? propriedade.Name;
                propriedade.SetColumnName(ParaSnakeCase(nomeColuna));
            }

            foreach (var chave in entidade.GetKeys())
            {
                var nome = chave.GetName();
                if (nome is not null) chave.SetName(ParaSnakeCase(nome));
            }

            foreach (var fk in entidade.GetForeignKeys())
            {
                var nome = fk.GetConstraintName();
                if (nome is not null) fk.SetConstraintName(ParaSnakeCase(nome));
            }

            foreach (var indice in entidade.GetIndexes())
            {
                var nome = indice.GetDatabaseName();
                if (nome is not null) indice.SetDatabaseName(ParaSnakeCase(nome));
            }
        }
    }

    /// <summary>"CodigoPostal" -> "codigo_postal"; "AspNetUserClaims" -> "asp_net_user_claims".</summary>
    public static string ParaSnakeCase(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return nome;

        var sb = new StringBuilder(nome.Length + 8);
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];

            if (char.IsUpper(c))
            {
                var anteriorMinuscula = i > 0 && (char.IsLower(nome[i - 1]) || char.IsDigit(nome[i - 1]));
                var seguinteMinuscula = i + 1 < nome.Length && char.IsLower(nome[i + 1]);
                var anteriorMaiuscula = i > 0 && char.IsUpper(nome[i - 1]);

                if (i > 0 && (anteriorMinuscula || (anteriorMaiuscula && seguinteMinuscula)))
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (c == ' ' || c == '-')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString();
    }
}
