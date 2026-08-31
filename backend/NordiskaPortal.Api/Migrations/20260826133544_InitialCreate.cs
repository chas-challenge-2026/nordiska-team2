using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordMd5 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavingsAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    AccountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_SavingsAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "SavingsAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "CreatedAt", "Email", "Name", "PasswordMd5" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "anna@example.com", "Anna Lindqvist", "482c811da5d5b4bc6d497ffa98491e38" },
                    { 2, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "erik@example.com", "Erik Johansson", "482c811da5d5b4bc6d497ffa98491e38" }
                });

            migrationBuilder.InsertData(
                table: "SavingsAccounts",
                columns: new[] { "Id", "AccountNumber", "AccountType", "Balance", "CreatedAt", "CustomerId", "InterestRate" },
                values: new object[,]
                {
                    { 1, "NKM-10001", "Savings", 125000.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0.0350m },
                    { 2, "NKM-10002", "Savings", 45000.00m, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0.0280m },
                    { 3, "NKM-20001", "Savings", 89500.00m, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), 2, 0.0350m }
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "AccountId", "Amount", "BalanceAfter", "PostingDate", "Status", "TransactionDate", "Type" },
                values: new object[,]
                {
                    { 1, 1, 125000.00m, 125000.00m, new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 2, 2, 45000.00m, 45000.00m, new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 3, 3, 89500.00m, 89500.00m, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Withdrawal" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavingsAccounts_CustomerId",
                table: "SavingsAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "SavingsAccounts");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
