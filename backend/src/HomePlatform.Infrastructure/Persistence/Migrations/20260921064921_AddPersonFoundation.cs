using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMember_AspNetUsers_AccountId",
                table: "HouseholdMember");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMember_AccountId",
                table: "HouseholdMember");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMember_HouseholdId_AccountId",
                table: "HouseholdMember");

            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                });

            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ADD COLUMN "PersonId" uuid;
                UPDATE "AspNetUsers" SET "PersonId" = gen_random_uuid();
                INSERT INTO "Person" ("Id") SELECT "PersonId" FROM "AspNetUsers";
                ALTER TABLE "AspNetUsers" ALTER COLUMN "PersonId" SET NOT NULL;

                ALTER TABLE "HouseholdMember" ADD COLUMN "PersonId" uuid;
                UPDATE "HouseholdMember" m SET "PersonId" = u."PersonId"
                FROM "AspNetUsers" u WHERE m."AccountId" = u."Id";
                UPDATE "HouseholdMember" SET "PersonId" = gen_random_uuid()
                WHERE "AccountId" IS NULL;
                INSERT INTO "Person" ("Id")
                SELECT "PersonId" FROM "HouseholdMember" WHERE "AccountId" IS NULL;
                ALTER TABLE "HouseholdMember" ALTER COLUMN "PersonId" SET NOT NULL;
                ALTER TABLE "HouseholdMember" DROP COLUMN "AccountId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_HouseholdId_PersonId",
                table: "HouseholdMember",
                columns: new[] { "HouseholdId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_PersonId",
                table: "HouseholdMember",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PersonId",
                table: "AspNetUsers",
                column: "PersonId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Person_PersonId",
                table: "AspNetUsers",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMember_Person_PersonId",
                table: "HouseholdMember",
                column: "PersonId",
                principalTable: "Person",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old model cannot represent an owner without an account. Refuse
            // that downgrade instead of inventing an account or changing a role.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "HouseholdMember" m
                        LEFT JOIN "AspNetUsers" u ON u."PersonId" = m."PersonId"
                        WHERE m."Role" = 0 AND u."Id" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade: an owner Person has no account';
                    END IF;
                END $$;
                ALTER TABLE "HouseholdMember" ADD COLUMN "AccountId" uuid;
                UPDATE "HouseholdMember" m SET "AccountId" = u."Id"
                FROM "AspNetUsers" u WHERE u."PersonId" = m."PersonId";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Person_PersonId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_HouseholdMember_Person_PersonId",
                table: "HouseholdMember");

            migrationBuilder.DropTable(
                name: "Person");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMember_HouseholdId_PersonId",
                table: "HouseholdMember");

            migrationBuilder.DropIndex(
                name: "IX_HouseholdMember_PersonId",
                table: "HouseholdMember");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PersonId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "HouseholdMember");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_AccountId",
                table: "HouseholdMember",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMember_HouseholdId_AccountId",
                table: "HouseholdMember",
                columns: new[] { "HouseholdId", "AccountId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HouseholdMember_AspNetUsers_AccountId",
                table: "HouseholdMember",
                column: "AccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
