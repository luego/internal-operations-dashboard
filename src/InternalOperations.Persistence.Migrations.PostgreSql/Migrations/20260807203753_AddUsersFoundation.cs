using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.PostgreSql.Migrations;

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
            type: "character varying(256)",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Users",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Users",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.Sql("UPDATE \"Users\" SET \"Version\" = gen_random_uuid() WHERE \"Version\" = '00000000-0000-0000-0000-000000000000'");

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
            filter: "\"NormalizedEmail\" IS NOT NULL");

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
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(256)",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Users",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
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
