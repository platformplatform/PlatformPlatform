﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlatformPlatform.AccountManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public sealed partial class AddAccountRegistrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountRegistrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(26)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(30)", nullable: false),
                    OneTimePassword = table.Column<string>(type: "varchar(6)", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRegistrations", x => x.Id);
                });
        }
    }
}