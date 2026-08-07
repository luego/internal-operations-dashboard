using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.PostgreSql.Migrations;

/// <inheritdoc />
public partial class CompleteTicketFoundation : Migration
{
    private static readonly string[] DepartmentStatusColumns = ["DepartmentId", "Status"];
    private static readonly string[] StatusPriorityCreatedColumns = ["Status", "Priority", "CreatedAtUtc"];
    private static readonly string[] UserStatusColumns = ["UserId", "Status"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Tickets_Departments_DepartmentId",
            table: "Tickets");

        migrationBuilder.DropForeignKey(
            name: "FK_Tickets_Users_UserId",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_DepartmentId",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_UserId",
            table: "Tickets");

        migrationBuilder.CreateSequence<int>(
            name: "TicketNumbers");

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "Tickets",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<int>(
            name: "Number",
            table: "Tickets",
            type: "integer",
            nullable: false,
            defaultValueSql: "nextval('\"TicketNumbers\"')",
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Tickets",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<Guid>(
            name: "Version",
            table: "Tickets",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.Sql("UPDATE \"Tickets\" SET \"Number\" = nextval('\"TicketNumbers\"'), \"Version\" = gen_random_uuid();");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_DepartmentId_Status",
            table: "Tickets",
            columns: DepartmentStatusColumns);

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_Number",
            table: "Tickets",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_Status_Priority_CreatedAtUtc",
            table: "Tickets",
            columns: StatusPriorityCreatedColumns);

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_UserId_Status",
            table: "Tickets",
            columns: UserStatusColumns);

        migrationBuilder.AddForeignKey(
            name: "FK_Tickets_Departments_DepartmentId",
            table: "Tickets",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Tickets_Users_UserId",
            table: "Tickets",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Tickets_Departments_DepartmentId",
            table: "Tickets");

        migrationBuilder.DropForeignKey(
            name: "FK_Tickets_Users_UserId",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_DepartmentId_Status",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_Number",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_Status_Priority_CreatedAtUtc",
            table: "Tickets");

        migrationBuilder.DropIndex(
            name: "IX_Tickets_UserId_Status",
            table: "Tickets");

        migrationBuilder.DropColumn(
            name: "Version",
            table: "Tickets");

        migrationBuilder.DropSequence(
            name: "TicketNumbers");

        migrationBuilder.AlterColumn<string>(
            name: "Title",
            table: "Tickets",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<int>(
            name: "Number",
            table: "Tickets",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValueSql: "nextval('\"TicketNumbers\"')");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Tickets",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(4000)",
            oldMaxLength: 4000);

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_DepartmentId",
            table: "Tickets",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Tickets_UserId",
            table: "Tickets",
            column: "UserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Tickets_Departments_DepartmentId",
            table: "Tickets",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_Tickets_Users_UserId",
            table: "Tickets",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id");
    }
}
