using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lex.Api.Migrations
{
    /// <inheritdoc />
    public partial class RelacionesUnoAUno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pago_id_trabajo",
                table: "pago");

            migrationBuilder.DropIndex(
                name: "IX_consentimiento_id_trabajo",
                table: "consentimiento");

            migrationBuilder.CreateIndex(
                name: "IX_pago_id_trabajo",
                table: "pago",
                column: "id_trabajo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_consentimiento_id_trabajo",
                table: "consentimiento",
                column: "id_trabajo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pago_id_trabajo",
                table: "pago");

            migrationBuilder.DropIndex(
                name: "IX_consentimiento_id_trabajo",
                table: "consentimiento");

            migrationBuilder.CreateIndex(
                name: "IX_pago_id_trabajo",
                table: "pago",
                column: "id_trabajo");

            migrationBuilder.CreateIndex(
                name: "IX_consentimiento_id_trabajo",
                table: "consentimiento",
                column: "id_trabajo");
        }
    }
}
