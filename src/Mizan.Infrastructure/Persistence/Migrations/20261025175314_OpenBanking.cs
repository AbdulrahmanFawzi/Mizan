using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mizan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpenBanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "bank_account_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "bank_connection_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_status",
                table: "transactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_source",
                table: "transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "transactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Debit");

            migrationBuilder.AddColumn<decimal>(
                name: "fee",
                table: "transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_cash_withdrawal",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_excluded",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "merchant_key",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "possible_duplicate_of",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_transaction_id",
                table: "transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "review",
                table: "transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "review_candidate_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "transactions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "treatment",
                table: "transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Spending");

            migrationBuilder.AddColumn<decimal>(
                name: "budget_amount",
                table: "transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                computedColumnSql: "CASE\n    WHEN is_excluded OR review = 'PossibleTransfer' OR possible_duplicate_of IS NOT NULL THEN 0\n    WHEN treatment = 'Spending' THEN amount + COALESCE(fee, 0)\n    WHEN treatment = 'Refund' THEN -amount\n    ELSE 0\nEND",
                stored: true);

            migrationBuilder.CreateTable(
                name: "bank_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bank_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_refresh_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    history_imported = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reminders_sent = table.Column<List<int>>(type: "integer[]", nullable: false),
                    consent_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consent_granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bank_customers",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_customer_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_customers", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_bank_customers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merchant_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchant_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_merchant_rules_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_merchant_rules_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rejected_matches",
                columns: table => new
                {
                    bank_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manual_transaction_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rejected_matches", x => new { x.bank_transaction_id, x.manual_transaction_id });
                    table.ForeignKey(
                        name: "fk_rejected_matches_transactions_bank_transaction_id",
                        column: x => x.bank_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rejected_matches_transactions_manual_transaction_id",
                        column: x => x.manual_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook_inbox",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_inbox", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    iban_encrypted = table.Column<string>(type: "text", nullable: true),
                    nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    include_in_budget = table.Column<bool>(type: "boolean", nullable: false),
                    balance_as_of = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_accounts_bank_connections_connection_id",
                        column: x => x.connection_id,
                        principalTable: "bank_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_bank_account_id",
                table: "transactions",
                column: "bank_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_bank_connection_id_provider_transaction_id",
                table: "transactions",
                columns: new[] { "bank_connection_id", "provider_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_id_merchant_key",
                table: "transactions",
                columns: new[] { "user_id", "merchant_key" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_connection_id_provider_account_id",
                table: "bank_accounts",
                columns: new[] { "connection_id", "provider_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_connections_provider_entity_id",
                table: "bank_connections",
                column: "provider_entity_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_connections_user_id_bank_identifier",
                table: "bank_connections",
                columns: new[] { "user_id", "bank_identifier" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_bank_customers_provider_customer_id",
                table: "bank_customers",
                column: "provider_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merchant_rules_category_id",
                table: "merchant_rules",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchant_rules_user_id_merchant_key",
                table: "merchant_rules",
                columns: new[] { "user_id", "merchant_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_next_attempt_at",
                table: "outbox_messages",
                columns: new[] { "processed_at", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rejected_matches_manual_transaction_id",
                table: "rejected_matches",
                column: "manual_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_inbox_processed_at_next_attempt_at",
                table: "webhook_inbox",
                columns: new[] { "processed_at", "next_attempt_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_bank_account_bank_account_id",
                table: "transactions",
                column: "bank_account_id",
                principalTable: "bank_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_bank_connections_bank_connection_id",
                table: "transactions",
                column: "bank_connection_id",
                principalTable: "bank_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_bank_account_bank_account_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "fk_transactions_bank_connections_bank_connection_id",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "bank_customers");

            migrationBuilder.DropTable(
                name: "merchant_rules");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "rejected_matches");

            migrationBuilder.DropTable(
                name: "webhook_inbox");

            migrationBuilder.DropTable(
                name: "bank_connections");

            migrationBuilder.DropIndex(
                name: "ix_transactions_bank_account_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_bank_connection_id_provider_transaction_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_user_id_merchant_key",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "budget_amount",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "bank_account_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "bank_connection_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "bank_status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "category_source",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "direction",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "fee",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "is_cash_withdrawal",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "is_excluded",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "merchant_key",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "note",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "possible_duplicate_of",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "provider_transaction_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "review",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "review_candidate_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "source",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "treatment",
                table: "transactions");
        }
    }
}
