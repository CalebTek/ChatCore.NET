using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatCore.Persistence.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToMessageRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the old single-column FK from MessageReads → Conversations
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReads_Conversations_ConversationId",
                table: "MessageReads");

            // 2. Add the TenantId column (nullable first so existing rows can be back-filled)
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "MessageReads",
                type: "uniqueidentifier",
                nullable: true);

            // 3. Back-fill TenantId from the joined Conversations row
            migrationBuilder.Sql(@"
                UPDATE mr
                SET mr.TenantId = c.TenantId
                FROM MessageReads mr
                INNER JOIN Conversations c ON c.Id = mr.ConversationId
            ");

            // 4. Now that all rows have a value, make the column non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "MessageReads",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 5. Add composite FK to Conversations (Id, TenantId)
            migrationBuilder.AddForeignKey(
                name: "FK_MessageReads_Conversations_ConversationId_TenantId",
                table: "MessageReads",
                columns: new[] { "ConversationId", "TenantId" },
                principalTable: "Conversations",
                principalColumns: new[] { "Id", "TenantId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove composite FK
            migrationBuilder.DropForeignKey(
                name: "FK_MessageReads_Conversations_ConversationId_TenantId",
                table: "MessageReads");

            // Remove TenantId column
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MessageReads");

            // Restore original single-column FK
            migrationBuilder.AddForeignKey(
                name: "FK_MessageReads_Conversations_ConversationId",
                table: "MessageReads",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
