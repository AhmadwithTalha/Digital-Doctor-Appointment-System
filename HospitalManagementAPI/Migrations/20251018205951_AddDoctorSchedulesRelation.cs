using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorSchedulesRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoctorId1",
                table: "DoctorSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoctorScheduleScheduleId",
                table: "DoctorSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DoctorSchedules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorId1",
                table: "DoctorSchedules",
                column: "DoctorId1");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSchedules_DoctorScheduleScheduleId",
                table: "DoctorSchedules",
                column: "DoctorScheduleScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorSchedules_DoctorSchedules_DoctorScheduleScheduleId",
                table: "DoctorSchedules",
                column: "DoctorScheduleScheduleId",
                principalTable: "DoctorSchedules",
                principalColumn: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorSchedules_Doctors_DoctorId1",
                table: "DoctorSchedules",
                column: "DoctorId1",
                principalTable: "Doctors",
                principalColumn: "DoctorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorSchedules_DoctorSchedules_DoctorScheduleScheduleId",
                table: "DoctorSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorSchedules_Doctors_DoctorId1",
                table: "DoctorSchedules");

            migrationBuilder.DropIndex(
                name: "IX_DoctorSchedules_DoctorId1",
                table: "DoctorSchedules");

            migrationBuilder.DropIndex(
                name: "IX_DoctorSchedules_DoctorScheduleScheduleId",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "DoctorId1",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "DoctorScheduleScheduleId",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DoctorSchedules");
        }
    }
}
