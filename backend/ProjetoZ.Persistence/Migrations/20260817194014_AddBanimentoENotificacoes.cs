using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanimentoENotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Banido",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BanidoEm",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BanidoMotivo",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificacaoDestinatarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacaoDestinatarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificacaoLeituras",
                columns: table => new
                {
                    NotificacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LidaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacaoLeituras", x => new { x.NotificacaoId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnviarEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParaTodos = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoDestinatarios_NotificacaoId_UserId",
                table: "NotificacaoDestinatarios",
                columns: new[] { "NotificacaoId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacaoDestinatarios_UserId",
                table: "NotificacaoDestinatarios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_EnviarEm",
                table: "Notificacoes",
                column: "EnviarEm");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_ExpiraEm",
                table: "Notificacoes",
                column: "ExpiraEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacaoDestinatarios");

            migrationBuilder.DropTable(
                name: "NotificacaoLeituras");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "Banido",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BanidoEm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BanidoMotivo",
                table: "Users");
        }
    }
}
