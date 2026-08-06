using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SteamId",
                table: "Users",
                newName: "Profile_SteamId");

            migrationBuilder.AlterColumn<string>(
                name: "Profile_SteamId",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Profile_Avatar",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profile_Name",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Profile_ProfileUrl",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Profile_Avatar",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Profile_Name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Profile_ProfileUrl",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Profile_SteamId",
                table: "Users",
                newName: "SteamId");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
