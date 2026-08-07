using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddDepartmentsFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Departments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Departments",
            type: "character varying(500)",
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "Departments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Departments",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "Departments"
            SET "NormalizedName" = UPPER(REGEXP_REPLACE(TRIM("Name"), '\s+', ' ', 'g')),
                "Version" = md5(random()::text || clock_timestamp()::text)::uuid;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "NormalizedName",
            table: "Departments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Version",
            table: "Departments",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Departments_NormalizedName",
            table: "Departments",
            column: "NormalizedName",
            unique: true,
            filter: "\"IsDeleted\" = FALSE");
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
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Departments",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500);
    }
}
