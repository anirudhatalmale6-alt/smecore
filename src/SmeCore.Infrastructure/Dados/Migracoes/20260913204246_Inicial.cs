using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmeCore.Infrastructure.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asp_net_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    de_sistema = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cargo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_acesso_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deve_alterar_palavra_passe = table.Column<bool>(type: "boolean", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    telemovel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    morada = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    codigo_postal = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    localidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    pais = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alterado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eliminado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registos_auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    instante = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    entidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidade_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidade_descricao = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    acao = table.Column<int>(type: "integer", nullable: false),
                    utilizador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    utilizador_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    origem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    alteracoes_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registos_auditoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "asp_net_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asp_net_user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cliente_contactos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    funcao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    principal = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alterado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eliminado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cliente_contactos", x => x.id);
                    table.ForeignKey(
                        name: "fk_cliente_contactos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "veiculos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    marca = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    versao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ano = table.Column<int>(type: "integer", nullable: true),
                    data_primeira_matricula = table.Column<DateOnly>(type: "date", nullable: true),
                    combustivel = table.Column<int>(type: "integer", nullable: false),
                    caixa = table.Column<int>(type: "integer", nullable: false),
                    cilindrada = table.Column<int>(type: "integer", nullable: true),
                    potencia = table.Column<int>(type: "integer", nullable: true),
                    cor = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    quilometragem = table.Column<int>(type: "integer", nullable: true),
                    proxima_inspecao = table.Column<DateOnly>(type: "date", nullable: true),
                    observacoes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    criado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alterado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alterado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false),
                    eliminado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    eliminado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    eliminado_por_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_veiculos", x => x.id);
                    table.ForeignKey(
                        name: "fk_veiculos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "asp_net_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "role_name_index",
                table: "asp_net_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "asp_net_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "asp_net_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "asp_net_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "email_index",
                table: "asp_net_users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_ativo",
                table: "asp_net_users",
                column: "ativo");

            migrationBuilder.CreateIndex(
                name: "user_name_index",
                table: "asp_net_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cliente_contactos_cliente_id",
                table: "cliente_contactos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_eliminado",
                table: "clientes",
                column: "eliminado");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nif_unico",
                table: "clientes",
                column: "nif",
                unique: true,
                filter: "nif IS NOT NULL AND eliminado = false");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_nome",
                table: "clientes",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "ix_clientes_numero",
                table: "clientes",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registos_auditoria_entidade_entidade_id",
                table: "registos_auditoria",
                columns: new[] { "entidade", "entidade_id" });

            migrationBuilder.CreateIndex(
                name: "ix_registos_auditoria_instante",
                table: "registos_auditoria",
                column: "instante");

            migrationBuilder.CreateIndex(
                name: "ix_registos_auditoria_utilizador_id",
                table: "registos_auditoria",
                column: "utilizador_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_cliente_id",
                table: "veiculos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_eliminado",
                table: "veiculos",
                column: "eliminado");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_marca",
                table: "veiculos",
                column: "marca");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_matricula_unica",
                table: "veiculos",
                column: "matricula",
                unique: true,
                filter: "eliminado = false");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_proxima_inspecao",
                table: "veiculos",
                column: "proxima_inspecao");

            migrationBuilder.CreateIndex(
                name: "ix_veiculos_vin_unico",
                table: "veiculos",
                column: "vin",
                unique: true,
                filter: "vin IS NOT NULL AND eliminado = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "cliente_contactos");

            migrationBuilder.DropTable(
                name: "registos_auditoria");

            migrationBuilder.DropTable(
                name: "veiculos");

            migrationBuilder.DropTable(
                name: "asp_net_roles");

            migrationBuilder.DropTable(
                name: "asp_net_users");

            migrationBuilder.DropTable(
                name: "clientes");
        }
    }
}
