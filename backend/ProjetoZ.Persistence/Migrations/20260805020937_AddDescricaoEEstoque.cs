using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDescricaoEEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Products",
                newName: "Estoque");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "Preco");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Products",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Products",
                newName: "Imagem");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "Preco",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Products",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Imagem",
                table: "Products",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Estoque",
                table: "Products",
                newName: "Stock");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
