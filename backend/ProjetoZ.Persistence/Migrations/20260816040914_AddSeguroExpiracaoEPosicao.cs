using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoZ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeguroExpiracaoEPosicao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarroId",
                table: "Seguros",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiraEm",
                table: "Seguros",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PosicaoAtualizadaEm",
                table: "Seguros",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosicaoGrid",
                table: "Seguros",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PosicaoX",
                table: "Seguros",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PosicaoZ",
                table: "Seguros",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VeiculoNome",
                table: "Seguros",
                type: "text",
                nullable: true);

            // Linhas já existentes ganham o padrão temporário 0001-01-01 na
            // coluna nova (NOT NULL sem valor real ainda) — sem isso todo
            // seguro já ativo em produção "expiraria" no instante em que essa
            // migration rodasse. Backfill: 1 mês a partir da criação, igual
            // regra usada pros seguros novos.
            migrationBuilder.Sql(
                "UPDATE \"Seguros\" SET \"ExpiraEm\" = \"CriadoEm\" + INTERVAL '1 month';");

            migrationBuilder.CreateIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros",
                column: "CarroId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seguros_CarroId",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "CarroId",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "ExpiraEm",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "PosicaoAtualizadaEm",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "PosicaoGrid",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "PosicaoX",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "PosicaoZ",
                table: "Seguros");

            migrationBuilder.DropColumn(
                name: "VeiculoNome",
                table: "Seguros");
        }
    }
}
