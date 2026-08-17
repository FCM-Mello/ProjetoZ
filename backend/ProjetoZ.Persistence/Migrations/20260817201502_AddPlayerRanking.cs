using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kills = table.Column<int>(type: "integer", nullable: false),
                    Deaths = table.Column<int>(type: "integer", nullable: false),
                    KothCompletados = table.Column<int>(type: "integer", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRankings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRankings_UserId",
                table: "PlayerRankings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerRankings");
        }
    }
}
