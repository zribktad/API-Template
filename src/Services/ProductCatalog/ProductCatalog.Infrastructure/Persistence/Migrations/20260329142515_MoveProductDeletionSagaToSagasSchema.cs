using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveProductDeletionSagaToSagasSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "sagas");

            migrationBuilder.RenameTable(
                name: "ProductDeletionSagas",
                newName: "ProductDeletionSagas",
                newSchema: "sagas"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ProductDeletionSagas",
                schema: "sagas",
                newName: "ProductDeletionSagas"
            );
        }
    }
}
