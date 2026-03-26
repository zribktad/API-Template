using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileStorage.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(
                        type: "character varying(255)",
                        maxLength: 255,
                        nullable: false
                    ),
                    StoragePath = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    ContentType = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    CreatedBy = table.Column<Guid>(
                        type: "uuid",
                        nullable: false,
                        defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
                    ),
                    UpdatedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false,
                        defaultValueSql: "now()"
                    ),
                    UpdatedBy = table.Column<Guid>(
                        type: "uuid",
                        nullable: false,
                        defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
                    ),
                    IsDeleted = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: false
                    ),
                    DeletedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                    table.CheckConstraint(
                        "CK_StoredFiles_SoftDeleteConsistency",
                        "\"IsDeleted\" OR (\"DeletedAtUtc\" IS NULL AND \"DeletedBy\" IS NULL)"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_TenantId",
                table: "StoredFiles",
                column: "TenantId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_TenantId_IsDeleted",
                table: "StoredFiles",
                columns: new[] { "TenantId", "IsDeleted" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StoredFiles");
        }
    }
}
