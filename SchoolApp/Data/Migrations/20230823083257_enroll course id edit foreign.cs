﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolApp.Data.Migrations
{
    public partial class enrollcourseideditforeign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EUserName",
                table: "Enrollments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EUserName",
                table: "Enrollments");
        }
    }
}