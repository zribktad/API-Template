using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveTenantDeactivationSagaToSagasSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "sagas");

            migrationBuilder.RenameTable(
                name: "TenantDeactivationSagas",
                newName: "TenantDeactivationSagas",
                newSchema: "sagas"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "TenantDeactivationSagas",
                schema: "sagas",
                newName: "TenantDeactivationSagas"
            );
        }
    }
}
