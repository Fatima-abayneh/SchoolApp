using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.Data.Migrations
{
    public partial class subbss15 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubmmited",
                table: "AssignmentSubmissions");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmitted",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubmitted",
                table: "Assignments");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmmited",
                table: "AssignmentSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
