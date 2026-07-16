using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lex.Api.Migrations
{
    /// <inheritdoc />
    public partial class TurnosYSesiones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disponibilidad_estudiante",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estudiante_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<string>(type: "text", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disponibilidad_estudiante", x => x.id);
                    table.ForeignKey(
                        name: "FK_disponibilidad_estudiante_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "turno",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estudiante_id = table.Column<int>(type: "integer", nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_hora_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duracion_minutos = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    link_videollamada = table.Column<string>(type: "text", nullable: true),
                    notas_estudiante = table.Column<string>(type: "text", nullable: true),
                    notas_cliente = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turno", x => x.id);
                    table.ForeignKey(
                        name: "FK_turno_perfil_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_turno_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sesion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trabajo_id = table.Column<int>(type: "integer", nullable: false),
                    turno_id = table.Column<int>(type: "integer", nullable: false),
                    numero_sesion = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    fecha_realizada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion", x => x.id);
                    table.ForeignKey(
                        name: "FK_sesion_trabajo_trabajo_id",
                        column: x => x.trabajo_id,
                        principalTable: "trabajo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sesion_turno_turno_id",
                        column: x => x.turno_id,
                        principalTable: "turno",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disponibilidad_estudiante_estudiante_id_dia_semana",
                table: "disponibilidad_estudiante",
                columns: new[] { "estudiante_id", "dia_semana" });

            migrationBuilder.CreateIndex(
                name: "IX_sesion_trabajo_id",
                table: "sesion",
                column: "trabajo_id");

            migrationBuilder.CreateIndex(
                name: "IX_sesion_turno_id",
                table: "sesion",
                column: "turno_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_turno_cliente_id_fecha_hora_inicio",
                table: "turno",
                columns: new[] { "cliente_id", "fecha_hora_inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_turno_estudiante_id_fecha_hora_inicio",
                table: "turno",
                columns: new[] { "estudiante_id", "fecha_hora_inicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disponibilidad_estudiante");

            migrationBuilder.DropTable(
                name: "sesion");

            migrationBuilder.DropTable(
                name: "turno");
        }
    }
}
