using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BairrOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProtegeTipoCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VERSAO",
                table: "BRN_CATEGORIAS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VERSAO",
                table: "BRN_CATEGORIAS");
        }
    }
}
