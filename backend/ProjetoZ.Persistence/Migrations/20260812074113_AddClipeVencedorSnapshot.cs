using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClipeVencedorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UltimoVencedorAutorAvatar",
                table: "ClipeConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoVencedorAutorNome",
                table: "ClipeConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UltimoVencedorCurtidas",
                table: "ClipeConfigs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoVencedorFechadoEm",
                table: "ClipeConfigs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoVencedorTitulo",
                table: "ClipeConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoVencedorUrl",
                table: "ClipeConfigs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UltimoVencedorAutorAvatar",
                table: "ClipeConfigs");

            migrationBuilder.DropColumn(
                name: "UltimoVencedorAutorNome",
                table: "ClipeConfigs");

            migrationBuilder.DropColumn(
                name: "UltimoVencedorCurtidas",
                table: "ClipeConfigs");

            migrationBuilder.DropColumn(
                name: "UltimoVencedorFechadoEm",
                table: "ClipeConfigs");

            migrationBuilder.DropColumn(
                name: "UltimoVencedorTitulo",
                table: "ClipeConfigs");

            migrationBuilder.DropColumn(
                name: "UltimoVencedorUrl",
                table: "ClipeConfigs");
        }
    }
}
