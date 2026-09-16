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
                    PersonalId = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavingsAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                name: "TaxReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    ReportId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PdfData = table.Column<byte[]>(type: "bytea", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxReports_SavingsAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "SavingsAccounts",
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
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(15,2)", nullable: false),
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
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "Name", "PasswordHash", "PersonalId" },
                values: new object[,]
                {
                    { 1, "Storgatan 1, 111 22 Stockholm", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "anna@example.com", "Anna Lindqvist", "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO", "19850505-1234" },
                    { 2, "Kungsgatan 5, 411 19 Göteborg", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "erik@example.com", "Erik Johansson", "$2b$12$hg6bJTmUyy.QTahIR9LWf.6vdXcGceKXaMd0r4mOeVbyvAAeX8vEO", "19991212-5678" }
                });

            migrationBuilder.InsertData(
                table: "SavingsAccounts",
                columns: new[] { "Id", "AccountNumber", "AccountType", "CreatedAt", "CustomerId", "InterestRate" },
                values: new object[,]
                {
                    { 1, "NKM-10001", "Savings", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0.0350m },
                    { 2, "NKM-10002", "Savings", new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), 1, 0.0280m },
                    { 3, "NKM-20001", "Savings", new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), 2, 0.0350m }
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "AccountId", "Amount", "Description", "PostingDate", "Status", "TransactionDate", "Type" },
                values: new object[,]
                {
                    { 1, 1, 125000.00m, "Lön", new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 2, 2, 45000.00m, "Swish", new DateTime(2026, 2, 4, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 2, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 3, 3, 89500.00m, "Lön", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 4, 1, 89500.00m, "Swish", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2025, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 5, 1, 40500.00m, "Lön", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2025, 4, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" },
                    { 6, 1, 20000.00m, "Semester", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2025, 5, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Withdrawal" },
                    { 7, 1, 50000.00m, "Lön", new DateTime(2023, 1, 17, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2023, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CustomerId",
                table: "RefreshTokens",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsAccounts_CustomerId",
                table: "SavingsAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxReports_AccountId_Year",
                table: "TaxReports",
                columns: new[] { "AccountId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TaxReports");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "SavingsAccounts");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
