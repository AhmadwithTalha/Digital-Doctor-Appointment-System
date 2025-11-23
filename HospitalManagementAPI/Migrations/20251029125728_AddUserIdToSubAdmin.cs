using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToSubAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SubAdmins",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubAdmins_UserId",
                table: "SubAdmins",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubAdmins_Users_UserId",
                table: "SubAdmins",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubAdmins_Users_UserId",
                table: "SubAdmins");

            migrationBuilder.DropIndex(
                name: "IX_SubAdmins_UserId",
                table: "SubAdmins");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SubAdmins");
        }
    }
}
