using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APITemplate.Infrastructure.Migrations.TickerQ
{
    /// <inheritdoc />
    public partial class SyncTickerQSchedulerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                schema: "tickerq",
                table: "CronTickers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsEnabled",
                schema: "tickerq",
                table: "CronTickers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean"
            );
        }
    }
}
