using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjustarIndiceCarroIdSeguro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros");

            migrationBuilder.CreateIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros",
                column: "CarroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros");

            migrationBuilder.CreateIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros",
                column: "CarroId",
                unique: true);
        }
    }
}
