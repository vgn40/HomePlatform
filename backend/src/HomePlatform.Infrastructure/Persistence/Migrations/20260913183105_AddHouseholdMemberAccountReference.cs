using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdMemberAccountReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_AccountId",
                table: "HouseholdMember",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMember_AspNetUsers_AccountId",
                table: "HouseholdMember",
                column: "AccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMember_AspNetUsers_AccountId",
                table: "HouseholdMember");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMember_AccountId",
                table: "HouseholdMember");
        }
    }
}
