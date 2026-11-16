using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BairrOn.Api.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // cria a tabela de usuarios como pedido no pdf 
            migrationBuilder.CreateTable(
                name: "BRN_USUARIOS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(254)", maxLength: 254, nullable: false),
                    EMAILNORMALIZADO = table.Column<string>(type: "NVARCHAR2(254)", maxLength: 254, nullable: false),
                    SENHAHASH = table.Column<string>(type: "NVARCHAR2(512)", maxLength: 512, nullable: false),
                    VERSAOTOKEN = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BRN_USUARIOS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BRN_CATEGORIAS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    TIPO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CRIADORID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BRN_CATEGORIAS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BRN_CATEGORIAS_BRN_USUARIOS_CRIADORID",
                        column: x => x.CRIADORID,
                        principalTable: "BRN_USUARIOS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BRN_PUBLICACOES",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TITULO = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    ENDERECO = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    PONTOREFERENCIA = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    CATEGORIAID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AUTORID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ANONIMA = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CRIADAEM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    INICIOEM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    ENCERRAMENTOEM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    RESOLVIDAEM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BRN_PUBLICACOES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BRN_PUBLICACOES_BRN_CATEGORIAS_CATEGORIAID",
                        column: x => x.CATEGORIAID,
                        principalTable: "BRN_CATEGORIAS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BRN_PUBLICACOES_BRN_USUARIOS_AUTORID",
                        column: x => x.AUTORID,
                        principalTable: "BRN_USUARIOS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BRN_CATEGORIAS_CRIADORID",
                table: "BRN_CATEGORIAS",
                column: "CRIADORID");

            migrationBuilder.CreateIndex(
                name: "IX_BRN_PUBLICACOES_AUTORID",
                table: "BRN_PUBLICACOES",
                column: "AUTORID");

            migrationBuilder.CreateIndex(
                name: "IX_BRN_PUBLICACOES_CATEGORIAID",
                table: "BRN_PUBLICACOES",
                column: "CATEGORIAID");

            migrationBuilder.CreateIndex(
                name: "IX_BRN_USUARIOS_EMAILNORMALIZADO",
                table: "BRN_USUARIOS",
                column: "EMAILNORMALIZADO",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BRN_PUBLICACOES");

            migrationBuilder.DropTable(
                name: "BRN_CATEGORIAS");

            migrationBuilder.DropTable(
                name: "BRN_USUARIOS");
        }
    }
}
