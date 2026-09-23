using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoicingApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DeletedAt", "DeletedBy", "PasswordHash", "Role", "Username", "Version" },
                values: new object[] { new Guid("01995c3a-0000-7000-8000-000000000001"), null, null, "$2a$11$ouzQd7TvlOMBYHCZDzZ0oOXVoJhzDzl7NAM91wOFYS/HGgwR3iHEa", "Admin", "Admin", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("01995c3a-0000-7000-8000-000000000001"));
        }
    }
}
