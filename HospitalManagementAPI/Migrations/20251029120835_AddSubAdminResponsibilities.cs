using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSubAdminResponsibilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "SubAdmins",
                newName: "Responsibilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Responsibilities",
                table: "SubAdmins",
                newName: "Role");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SubAdmins",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                onDelete: ReferentialAction.Cascade);
        }
    }
}
