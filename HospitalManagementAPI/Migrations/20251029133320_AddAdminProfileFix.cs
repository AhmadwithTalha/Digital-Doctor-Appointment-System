using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminProfileFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AdminProfiles",
                newName: "SystemEmail");

            migrationBuilder.AddColumn<string>(
                name: "PersonalEmail",
                table: "AdminProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalEmail",
                table: "AdminProfiles");

            migrationBuilder.RenameColumn(
                name: "SystemEmail",
                table: "AdminProfiles",
                newName: "Email");
        }
    }
}
