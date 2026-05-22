using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OsFacil.Migrations
{
   
    public partial class CriarTabelas : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OS_FUNCIONARIOS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Nome = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Salario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataAdmissao = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OS_FUNCIONARIOS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OS_USUARIOS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Nome = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "NVARCHAR2(15)", maxLength: 15, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OS_USUARIOS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OS_CARROS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Marca = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Modelo = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Ano = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Placa = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: false),
                    UsuarioId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OS_CARROS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OS_CARROS_OS_USUARIOS_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "OS_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OS_ORDEM_SERVICO",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Descricao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UsuarioId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    FuncionarioId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    CarroId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OS_ORDEM_SERVICO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OS_ORDEM_SERVICO_OS_CARROS_CarroId",
                        column: x => x.CarroId,
                        principalTable: "OS_CARROS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OS_ORDEM_SERVICO_OS_FUNCIONARIOS_FuncionarioId",
                        column: x => x.FuncionarioId,
                        principalTable: "OS_FUNCIONARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OS_ORDEM_SERVICO_OS_USUARIOS_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "OS_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OS_ITEMSERVICO",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Descricao = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrdemServicoId = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OS_ITEMSERVICO", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OS_ITEMSERVICO_OS_ORDEM_SERVICO_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OS_ORDEM_SERVICO",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OS_CARROS_UsuarioId",
                table: "OS_CARROS",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OS_ITEMSERVICO_OrdemServicoId",
                table: "OS_ITEMSERVICO",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_OS_ORDEM_SERVICO_CarroId",
                table: "OS_ORDEM_SERVICO",
                column: "CarroId");

            migrationBuilder.CreateIndex(
                name: "IX_OS_ORDEM_SERVICO_FuncionarioId",
                table: "OS_ORDEM_SERVICO",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OS_ORDEM_SERVICO_UsuarioId",
                table: "OS_ORDEM_SERVICO",
                column: "UsuarioId");
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OS_ITEMSERVICO");

            migrationBuilder.DropTable(
                name: "OS_ORDEM_SERVICO");

            migrationBuilder.DropTable(
                name: "OS_CARROS");

            migrationBuilder.DropTable(
                name: "OS_FUNCIONARIOS");

            migrationBuilder.DropTable(
                name: "OS_USUARIOS");
        }
    }
}
