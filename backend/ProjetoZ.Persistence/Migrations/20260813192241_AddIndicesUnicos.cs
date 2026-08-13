using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicesUnicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Profile_SteamId",
                table: "Users",
                column: "Profile_SteamId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_YoutubeChannelId",
                table: "Users",
                column: "YoutubeChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Categoria",
                table: "Products",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Categorys_Nome",
                table: "Categorys",
                column: "Nome",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Profile_SteamId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_YoutubeChannelId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_Categoria",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categorys_Nome",
                table: "Categorys");
        }
    }
}
