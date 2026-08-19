using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClasUnificado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SegundosJogados",
                table: "PlayerRankings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZumbiKills",
                table: "PlayerRankings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClaMembros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SteamId = table.Column<string>(type: "text", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    EntrouEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaMembros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrupoModId = table.Column<string>(type: "text", nullable: true),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Estandarte = table.Column<string>(type: "text", nullable: true),
                    LiderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LiderSteamId = table.Column<string>(type: "text", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClaSolicitacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaSolicitacoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaMembros_ClaId",
                table: "ClaMembros",
                column: "ClaId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaMembros_SteamId",
                table: "ClaMembros",
                column: "SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clas_GrupoModId",
                table: "Clas",
                column: "GrupoModId",
                unique: true,
                filter: "\"GrupoModId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clas_Nome",
                table: "Clas",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaSolicitacoes_ClaId_UserId",
                table: "ClaSolicitacoes",
                columns: new[] { "ClaId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaSolicitacoes_UserId",
                table: "ClaSolicitacoes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaMembros");

            migrationBuilder.DropTable(
                name: "Clas");

            migrationBuilder.DropTable(
                name: "ClaSolicitacoes");

            migrationBuilder.DropColumn(
                name: "SegundosJogados",
                table: "PlayerRankings");

            migrationBuilder.DropColumn(
                name: "ZumbiKills",
                table: "PlayerRankings");
        }
    }
}
