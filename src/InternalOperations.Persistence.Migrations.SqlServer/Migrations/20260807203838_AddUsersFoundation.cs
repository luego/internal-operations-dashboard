using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.SqlServer.Migrations;

/// <inheritdoc />
public partial class AddUsersFoundation : Migration
{
    private static readonly string[] DepartmentStatusColumns = ["DepartmentId", "IsActive"];
    private static readonly string[] StatusDisplayNameColumns = ["IsActive", "DisplayName"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_Departments_DepartmentId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_DepartmentId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "IdentityUsers");

        migrationBuilder.AlterColumn<string>(
            name: "UserName",
            table: "Users",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Users",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Users",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.Sql("UPDATE [Users] SET [Version] = NEWID() WHERE [Version] = '00000000-0000-0000-0000-000000000000'");

        migrationBuilder.CreateIndex(
            name: "IX_Users_DepartmentId_IsActive",
            table: "Users",
            columns: DepartmentStatusColumns);

        migrationBuilder.CreateIndex(
            name: "IX_Users_IsActive_DisplayName",
            table: "Users",
            columns: StatusDisplayNameColumns);

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "IdentityUsers",
            column: "NormalizedEmail",
            unique: true,
            filter: "[NormalizedEmail] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Departments_DepartmentId",
            table: "Users",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_Departments_DepartmentId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_DepartmentId_IsActive",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_IsActive_DisplayName",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "EmailIndex",
            table: "IdentityUsers");

        migrationBuilder.DropColumn(
            name: "Version",
            table: "Users");

        migrationBuilder.AlterColumn<string>(
            name: "UserName",
            table: "Users",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(256)",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Users",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(200)",
            oldMaxLength: 200);

        migrationBuilder.CreateIndex(
            name: "IX_Users_DepartmentId",
            table: "Users",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "IdentityUsers",
            column: "NormalizedEmail");

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Departments_DepartmentId",
            table: "Users",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id");
    }
}
