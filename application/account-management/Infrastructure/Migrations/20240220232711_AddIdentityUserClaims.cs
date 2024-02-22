using System;
using System.CodeDom.Compiler;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformPlatform.AccountManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [GeneratedCode("Entity Framework", null)]
    public sealed partial class AddIdentityUserClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "varchar(26)", nullable: true),
                    ClaimType = table.Column<string>(type: "varchar(100)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(1024)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUserClaims", x => x.Id);
                });
        }
    }
}
