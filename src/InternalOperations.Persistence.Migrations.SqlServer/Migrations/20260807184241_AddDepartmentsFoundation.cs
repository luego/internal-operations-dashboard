using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.SqlServer.Migrations;

/// <inheritdoc />
public partial class AddDepartmentsFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Departments",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Departments",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "Departments",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Departments",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE [Departments]
            SET [NormalizedName] = UPPER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE([Name], CHAR(9), ' '), CHAR(10), ' '), CHAR(13), ' ')))),
                [Version] = NEWID();
            WHILE EXISTS (SELECT 1 FROM [Departments] WHERE [NormalizedName] LIKE '%  %')
                UPDATE [Departments] SET [NormalizedName] = REPLACE([NormalizedName], '  ', ' ');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "NormalizedName",
            table: "Departments",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Version",
            table: "Departments",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Departments_NormalizedName",
            table: "Departments",
            column: "NormalizedName",
            unique: true,
            filter: "[IsDeleted] = 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Departments_NormalizedName",
            table: "Departments");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "Departments");

        migrationBuilder.DropColumn(
            name: "Version",
            table: "Departments");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Departments",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Departments",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500);
    }
}
