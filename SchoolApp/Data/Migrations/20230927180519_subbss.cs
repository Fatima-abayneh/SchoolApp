using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.Data.Migrations
{
    public partial class subbss : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnnouncementDocFile",
                table: "AssignmentSubmissions",
                newName: "SubmissionDocFile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubmissionDocFile",
                table: "AssignmentSubmissions",
                newName: "AnnouncementDocFile");
        }
    }
}
