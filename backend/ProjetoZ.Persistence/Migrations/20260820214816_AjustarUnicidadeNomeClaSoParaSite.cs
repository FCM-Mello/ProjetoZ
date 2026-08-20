using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjustarUnicidadeNomeClaSoParaSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clas_Nome",
                table: "Clas");

            migrationBuilder.CreateIndex(
                name: "IX_Clas_Nome",
                table: "Clas",
                column: "Nome",
                unique: true,
                filter: "\"GrupoModId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clas_Nome",
                table: "Clas");

            migrationBuilder.CreateIndex(
                name: "IX_Clas_Nome",
                table: "Clas",
                column: "Nome",
                unique: true);
        }
    }
}
