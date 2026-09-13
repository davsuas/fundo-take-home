using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fundo.Loans.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "blacklisted_ssns",
            columns: table => new
            {
                ssn_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blacklisted_ssns", x => x.ssn_hash);
            });

        migrationBuilder.CreateTable(
            name: "customers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                postal_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ssn_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ssn_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_customers", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                attempts = table.Column<int>(type: "integer", nullable: false),
                next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                dead_lettered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "loan_applications",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                requested_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_loan_applications", x => x.id);
                table.ForeignKey(
                    name: "FK_loan_applications_customers_customer_id",
                    column: x => x.customer_id,
                    principalTable: "customers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_customers_ssn_hash",
            table: "customers",
            column: "ssn_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_loan_applications_customer_id",
            table: "loan_applications",
            column: "customer_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_outbox_messages_processed_at_next_attempt_at",
            table: "outbox_messages",
            columns: new[] { "processed_at", "next_attempt_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "blacklisted_ssns");

        migrationBuilder.DropTable(
            name: "loan_applications");

        migrationBuilder.DropTable(
            name: "outbox_messages");

        migrationBuilder.DropTable(
            name: "customers");
    }
}
