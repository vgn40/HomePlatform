using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHousehold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Household",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Household", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMember",
                columns: table => new
                {
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMember", x => x.MembershipId);
                    table.ForeignKey(
                        name: "FK_HouseholdMember_Household_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Household",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_HouseholdId_AccountId",
                table: "HouseholdMember",
                columns: new[] { "HouseholdId", "AccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdMember");

            migrationBuilder.DropTable(
                name: "Household");
        }
    }
}
