using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWallet.Migrations
{
    /// <inheritdoc />
    public partial class frozenWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Wallets",
                newName: "IsFrozen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsFrozen",
                table: "Wallets",
                newName: "Status");
        }
    }
}
