using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternalOperations.Persistence.Migrations.SqlServer.Migrations;

/// <inheritdoc />
public partial class AddTicketCollaboration : Migration
{
    private static readonly string[] TicketCommentIndexColumns = ["TicketId", "CreatedAtUtc", "Id"];
    private static readonly string[] TicketActivityIndexColumns = ["TicketId", "OccurredAtUtc", "Id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TicketComments_Tickets_TicketId",
            table: "TicketComments");

        migrationBuilder.DropForeignKey(
            name: "FK_TicketComments_Users_UserId",
            table: "TicketComments");

        migrationBuilder.DropIndex(
            name: "IX_TicketComments_TicketId",
            table: "TicketComments");

        migrationBuilder.CreateTable(
            name: "TicketActivities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TicketActivities", x => x.Id);
                table.ForeignKey(
                    name: "FK_TicketActivities_Tickets_TicketId",
                    column: x => x.TicketId,
                    principalTable: "Tickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TicketComments_TicketId_CreatedAtUtc_Id",
            table: "TicketComments",
            columns: TicketCommentIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_TicketActivities_TicketId_OccurredAtUtc_Id",
            table: "TicketActivities",
            columns: TicketActivityIndexColumns);

        migrationBuilder.AddForeignKey(
            name: "FK_TicketComments_Tickets_TicketId",
            table: "TicketComments",
            column: "TicketId",
            principalTable: "Tickets",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_TicketComments_Users_UserId",
            table: "TicketComments",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TicketComments_Tickets_TicketId",
            table: "TicketComments");

        migrationBuilder.DropForeignKey(
            name: "FK_TicketComments_Users_UserId",
            table: "TicketComments");

        migrationBuilder.DropTable(
            name: "TicketActivities");

        migrationBuilder.DropIndex(
            name: "IX_TicketComments_TicketId_CreatedAtUtc_Id",
            table: "TicketComments");

        migrationBuilder.CreateIndex(
            name: "IX_TicketComments_TicketId",
            table: "TicketComments",
            column: "TicketId");

        migrationBuilder.AddForeignKey(
            name: "FK_TicketComments_Tickets_TicketId",
            table: "TicketComments",
            column: "TicketId",
            principalTable: "Tickets",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_TicketComments_Users_UserId",
            table: "TicketComments",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
