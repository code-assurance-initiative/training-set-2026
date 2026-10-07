using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quellbrook.Notifier.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    MaskedRecipient = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "recipients",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipients", x => x.OrderId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_log_OrderId_Kind_Channel",
                table: "notification_log",
                columns: new[] { "OrderId", "Kind", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_log_SentAt",
                table: "notification_log",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_processed_messages_ProcessedAt",
                table: "processed_messages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_recipients_CompletedAt",
                table: "recipients",
                column: "CompletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_log");

            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropTable(
                name: "recipients");
        }
    }
}
