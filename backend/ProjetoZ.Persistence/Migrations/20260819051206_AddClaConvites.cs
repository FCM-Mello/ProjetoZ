using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaConvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClaConviteId",
                table: "Notificacoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "text",
                nullable: false,
                defaultValue: "aviso");

            migrationBuilder.CreateTable(
                name: "ClaConvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvidadoUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvidadoPorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaConvites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaConvites_ClaId_ConvidadoUserId",
                table: "ClaConvites",
                columns: new[] { "ClaId", "ConvidadoUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaConvites_ConvidadoUserId",
                table: "ClaConvites",
                column: "ConvidadoUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaConvites");

            migrationBuilder.DropColumn(
                name: "ClaConviteId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Notificacoes");
        }
    }
}
