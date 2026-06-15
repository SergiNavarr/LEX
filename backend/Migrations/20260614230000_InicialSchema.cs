using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lex.Api.Migrations
{
    /// <inheritdoc />
    public partial class InicialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.rol_id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_institucion",
                columns: table => new
                {
                    tipo_institucion_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_institucion", x => x.tipo_institucion_id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_servicio",
                columns: table => new
                {
                    tipo_servicio_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    requiere_supervision = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_servicio", x => x.tipo_servicio_id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    email = table.Column<string>(type: "TEXT", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", nullable: false),
                    nombre_completo = table.Column<string>(type: "TEXT", nullable: false),
                    telefono = table.Column<string>(type: "TEXT", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.usuario_id);
                });

            migrationBuilder.CreateTable(
                name: "institucion",
                columns: table => new
                {
                    institucion_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    tipo_institucion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    provincia = table.Column<string>(type: "TEXT", nullable: true),
                    ciudad = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_institucion", x => x.institucion_id);
                    table.ForeignKey(
                        name: "FK_institucion_tipo_institucion_tipo_institucion_id",
                        column: x => x.tipo_institucion_id,
                        principalTable: "tipo_institucion",
                        principalColumn: "tipo_institucion_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_agencia",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre_agencia = table.Column<string>(type: "TEXT", nullable: true),
                    rubro = table.Column<string>(type: "TEXT", nullable: true),
                    sitio_web = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_agencia", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_perfil_agencia_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_cliente",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo_cliente = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_cliente", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_perfil_cliente_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "perfil_estudiante",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    bio = table.Column<string>(type: "TEXT", nullable: true),
                    anio_cursado = table.Column<int>(type: "INTEGER", nullable: true),
                    calificacion_promedio = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    cantidad_trabajos = table.Column<int>(type: "INTEGER", nullable: false),
                    disponible = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_estudiante", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_perfil_estudiante_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false),
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_rol", x => new { x.rol_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_usuario_rol_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_rol_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "carrera",
                columns: table => new
                {
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    institucion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    area_conocimiento = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrera", x => x.carrera_id);
                    table.ForeignKey(
                        name: "FK_carrera_institucion_institucion_id",
                        column: x => x.institucion_id,
                        principalTable: "institucion",
                        principalColumn: "institucion_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "datos_empresa",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    razon_social = table.Column<string>(type: "TEXT", nullable: true),
                    cuit = table.Column<string>(type: "TEXT", nullable: true),
                    rubro = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datos_empresa", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_datos_empresa_perfil_cliente_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "datos_particular",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    dni = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datos_particular", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_datos_particular_perfil_cliente_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    paciente_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cliente_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre_completo = table.Column<string>(type: "TEXT", nullable: false),
                    edad = table.Column<int>(type: "INTEGER", nullable: true),
                    notas = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.paciente_id);
                    table.ForeignKey(
                        name: "FK_pacientes_perfil_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitud",
                columns: table => new
                {
                    id_solicitud = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cliente_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo_servicio_id = table.Column<int>(type: "INTEGER", nullable: true),
                    titulo = table.Column<string>(type: "TEXT", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    presupuesto_estimado = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    estado = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fecha_cierre = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitud", x => x.id_solicitud);
                    table.ForeignKey(
                        name: "FK_solicitud_perfil_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_solicitud_tipo_servicio_tipo_servicio_id",
                        column: x => x.tipo_servicio_id,
                        principalTable: "tipo_servicio",
                        principalColumn: "tipo_servicio_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "servicio",
                columns: table => new
                {
                    id_servicio = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    estudiante_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo_servicio_id = table.Column<int>(type: "INTEGER", nullable: false),
                    titulo = table.Column<string>(type: "TEXT", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    precio = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    tiempo_entrega_dias = table.Column<int>(type: "INTEGER", nullable: true),
                    activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    fecha_publicacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servicio", x => x.id_servicio);
                    table.ForeignKey(
                        name: "FK_servicio_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_servicio_tipo_servicio_tipo_servicio_id",
                        column: x => x.tipo_servicio_id,
                        principalTable: "tipo_servicio",
                        principalColumn: "tipo_servicio_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estudiante_carrera",
                columns: table => new
                {
                    estudiante_id = table.Column<int>(type: "INTEGER", nullable: false),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    estado_verificacion = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha_verificacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    documento_comprobante = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estudiante_carrera", x => new { x.estudiante_id, x.carrera_id });
                    table.ForeignKey(
                        name: "FK_estudiante_carrera_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "carrera_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estudiante_carrera_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "postulacion",
                columns: table => new
                {
                    id_postulacion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_solicitud = table.Column<int>(type: "INTEGER", nullable: false),
                    estudiante_id = table.Column<int>(type: "INTEGER", nullable: false),
                    mensaje = table.Column<string>(type: "TEXT", nullable: true),
                    monto_propuesto = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    estado = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha_postulacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_postulacion", x => x.id_postulacion);
                    table.ForeignKey(
                        name: "FK_postulacion_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_postulacion_solicitud_id_solicitud",
                        column: x => x.id_solicitud,
                        principalTable: "solicitud",
                        principalColumn: "id_solicitud",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trabajo",
                columns: table => new
                {
                    id_trabajo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    estudiante_id = table.Column<int>(type: "INTEGER", nullable: false),
                    cliente_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tipo_servicio_id = table.Column<int>(type: "INTEGER", nullable: true),
                    origen = table.Column<int>(type: "INTEGER", nullable: false),
                    id_servicio = table.Column<int>(type: "INTEGER", nullable: true),
                    id_postulacion = table.Column<int>(type: "INTEGER", nullable: true),
                    paciente_id = table.Column<int>(type: "INTEGER", nullable: true),
                    estado = table.Column<int>(type: "INTEGER", nullable: false),
                    monto = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    fecha_fin = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trabajo", x => x.id_trabajo);
                    table.ForeignKey(
                        name: "FK_trabajo_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "paciente_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_perfil_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "perfil_cliente",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_perfil_estudiante_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "perfil_estudiante",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_postulacion_id_postulacion",
                        column: x => x.id_postulacion,
                        principalTable: "postulacion",
                        principalColumn: "id_postulacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_servicio_id_servicio",
                        column: x => x.id_servicio,
                        principalTable: "servicio",
                        principalColumn: "id_servicio",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_tipo_servicio_tipo_servicio_id",
                        column: x => x.tipo_servicio_id,
                        principalTable: "tipo_servicio",
                        principalColumn: "tipo_servicio_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consentimiento",
                columns: table => new
                {
                    id_consentimiento = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_trabajo = table.Column<int>(type: "INTEGER", nullable: false),
                    paciente_id = table.Column<int>(type: "INTEGER", nullable: true),
                    texto_consentimiento = table.Column<string>(type: "TEXT", nullable: true),
                    aceptado = table.Column<bool>(type: "INTEGER", nullable: false),
                    fecha_aceptacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    supervisor_responsable = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consentimiento", x => x.id_consentimiento);
                    table.ForeignKey(
                        name: "FK_consentimiento_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "paciente_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consentimiento_trabajo_id_trabajo",
                        column: x => x.id_trabajo,
                        principalTable: "trabajo",
                        principalColumn: "id_trabajo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pago",
                columns: table => new
                {
                    id_pago = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_trabajo = table.Column<int>(type: "INTEGER", nullable: false),
                    monto_total = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    porcentaje_comision = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    comision_lex = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    monto_estudiante = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    estado = table.Column<int>(type: "INTEGER", nullable: false),
                    metodo_pago = table.Column<string>(type: "TEXT", nullable: true),
                    fecha_retencion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    fecha_liberacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pago", x => x.id_pago);
                    table.ForeignKey(
                        name: "FK_pago_trabajo_id_trabajo",
                        column: x => x.id_trabajo,
                        principalTable: "trabajo",
                        principalColumn: "id_trabajo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resena",
                columns: table => new
                {
                    id_resena = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_trabajo = table.Column<int>(type: "INTEGER", nullable: false),
                    autor_usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    receptor_usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    puntaje = table.Column<int>(type: "INTEGER", nullable: false),
                    comentario = table.Column<string>(type: "TEXT", nullable: true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resena", x => x.id_resena);
                    table.ForeignKey(
                        name: "FK_resena_trabajo_id_trabajo",
                        column: x => x.id_trabajo,
                        principalTable: "trabajo",
                        principalColumn: "id_trabajo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_resena_usuario_autor_usuario_id",
                        column: x => x.autor_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_resena_usuario_receptor_usuario_id",
                        column: x => x.receptor_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trabajo_historial",
                columns: table => new
                {
                    id_historial = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_trabajo = table.Column<int>(type: "INTEGER", nullable: false),
                    estado_anterior = table.Column<int>(type: "INTEGER", nullable: true),
                    estado_nuevo = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trabajo_historial", x => x.id_historial);
                    table.ForeignKey(
                        name: "FK_trabajo_historial_trabajo_id_trabajo",
                        column: x => x.id_trabajo,
                        principalTable: "trabajo",
                        principalColumn: "id_trabajo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trabajo_historial_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carrera_institucion_id",
                table: "carrera",
                column: "institucion_id");

            migrationBuilder.CreateIndex(
                name: "IX_consentimiento_id_trabajo",
                table: "consentimiento",
                column: "id_trabajo");

            migrationBuilder.CreateIndex(
                name: "IX_consentimiento_paciente_id",
                table: "consentimiento",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_estudiante_carrera_carrera_id",
                table: "estudiante_carrera",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "IX_institucion_tipo_institucion_id",
                table: "institucion",
                column: "tipo_institucion_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacientes_cliente_id",
                table: "pacientes",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_pago_id_trabajo",
                table: "pago",
                column: "id_trabajo");

            migrationBuilder.CreateIndex(
                name: "IX_postulacion_estudiante_id",
                table: "postulacion",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "IX_postulacion_id_solicitud",
                table: "postulacion",
                column: "id_solicitud");

            migrationBuilder.CreateIndex(
                name: "IX_resena_autor_usuario_id",
                table: "resena",
                column: "autor_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_resena_id_trabajo_autor_usuario_id",
                table: "resena",
                columns: new[] { "id_trabajo", "autor_usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resena_receptor_usuario_id",
                table: "resena",
                column: "receptor_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_servicio_estudiante_id",
                table: "servicio",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "IX_servicio_tipo_servicio_id",
                table: "servicio",
                column: "tipo_servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_cliente_id",
                table: "solicitud",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_solicitud_tipo_servicio_id",
                table: "solicitud",
                column: "tipo_servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_cliente_id",
                table: "trabajo",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_estudiante_id",
                table: "trabajo",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_id_postulacion",
                table: "trabajo",
                column: "id_postulacion");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_id_servicio",
                table: "trabajo",
                column: "id_servicio");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_paciente_id",
                table: "trabajo",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_tipo_servicio_id",
                table: "trabajo",
                column: "tipo_servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_historial_id_trabajo",
                table: "trabajo_historial",
                column: "id_trabajo");

            migrationBuilder.CreateIndex(
                name: "IX_trabajo_historial_usuario_id",
                table: "trabajo_historial",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_usuario_id",
                table: "usuario_rol",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consentimiento");

            migrationBuilder.DropTable(
                name: "datos_empresa");

            migrationBuilder.DropTable(
                name: "datos_particular");

            migrationBuilder.DropTable(
                name: "estudiante_carrera");

            migrationBuilder.DropTable(
                name: "pago");

            migrationBuilder.DropTable(
                name: "perfil_agencia");

            migrationBuilder.DropTable(
                name: "resena");

            migrationBuilder.DropTable(
                name: "trabajo_historial");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "carrera");

            migrationBuilder.DropTable(
                name: "trabajo");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "institucion");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "postulacion");

            migrationBuilder.DropTable(
                name: "servicio");

            migrationBuilder.DropTable(
                name: "tipo_institucion");

            migrationBuilder.DropTable(
                name: "solicitud");

            migrationBuilder.DropTable(
                name: "perfil_estudiante");

            migrationBuilder.DropTable(
                name: "perfil_cliente");

            migrationBuilder.DropTable(
                name: "tipo_servicio");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
