using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "users",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmailVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "households",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_households_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "finance",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.CheckConstraint("ck_refresh_tokens_expiry_after_creation", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "finance",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.UniqueConstraint("AK_categories_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.CheckConstraint("ck_categories_not_own_parent", "\"ParentCategoryId\" IS NULL OR \"ParentCategoryId\" <> \"Id\"");
                    table.ForeignKey(
                        name: "FK_categories_categories_HouseholdId_ParentCategoryId",
                        columns: x => new { x.HouseholdId, x.ParentCategoryId },
                        principalSchema: "finance",
                        principalTable: "categories",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categories_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "household_members",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_members", x => x.Id);
                    table.UniqueConstraint("AK_household_members_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.ForeignKey(
                        name: "FK_household_members_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_household_members_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "finance",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budgets",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    month = table.Column<DateOnly>(type: "date", nullable: false),
                    limit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    limit_currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    alert_threshold_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budgets", x => x.Id);
                    table.CheckConstraint("ck_budgets_alert_threshold_valid", "\"alert_threshold_percent\" > 0 AND \"alert_threshold_percent\" <= 100");
                    table.CheckConstraint("ck_budgets_limit_positive", "\"limit_amount\" > 0");
                    table.CheckConstraint("ck_budgets_month_first_day", "EXTRACT(DAY FROM \"month\") = 1");
                    table.ForeignKey(
                        name: "FK_budgets_categories_HouseholdId_CategoryId",
                        columns: x => new { x.HouseholdId, x.CategoryId },
                        principalSchema: "finance",
                        principalTable: "categories",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budgets_household_members_HouseholdId_CreatedByMemberId",
                        columns: x => new { x.HouseholdId, x.CreatedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budgets_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Merchant = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                    table.UniqueConstraint("AK_expenses_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.CheckConstraint("ck_expenses_amount_positive", "\"amount_amount\" > 0");
                    table.ForeignKey(
                        name: "FK_expenses_categories_HouseholdId_CategoryId",
                        columns: x => new { x.HouseholdId, x.CategoryId },
                        principalSchema: "finance",
                        principalTable: "categories",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_household_members_HouseholdId_CreatedByMemberId",
                        columns: x => new { x.HouseholdId, x.CreatedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_expenses_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incomes",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incomes", x => x.Id);
                    table.UniqueConstraint("AK_incomes_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.CheckConstraint("ck_incomes_amount_positive", "\"amount_amount\" > 0");
                    table.ForeignKey(
                        name: "FK_incomes_categories_HouseholdId_CategoryId",
                        columns: x => new { x.HouseholdId, x.CategoryId },
                        principalSchema: "finance",
                        principalTable: "categories",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incomes_household_members_HouseholdId_CreatedByMemberId",
                        columns: x => new { x.HouseholdId, x.CreatedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incomes_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "savings_goals",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    target_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    target_currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_savings_goals", x => x.Id);
                    table.UniqueConstraint("AK_savings_goals_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.CheckConstraint("ck_savings_goals_target_positive", "\"target_amount\" > 0");
                    table.ForeignKey(
                        name: "FK_savings_goals_household_members_HouseholdId_CreatedByMember~",
                        columns: x => new { x.HouseholdId, x.CreatedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_savings_goals_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receipts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: false),
                    OriginalFilename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uuid", nullable: true),
                    IncomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipts", x => x.Id);
                    table.UniqueConstraint("AK_receipts_HouseholdId_Id", x => new { x.HouseholdId, x.Id });
                    table.CheckConstraint("ck_receipts_single_transaction", "\"ExpenseId\" IS NULL OR \"IncomeId\" IS NULL");
                    table.CheckConstraint("ck_receipts_size_positive", "\"SizeBytes\" > 0");
                    table.ForeignKey(
                        name: "FK_receipts_expenses_HouseholdId_ExpenseId",
                        columns: x => new { x.HouseholdId, x.ExpenseId },
                        principalSchema: "finance",
                        principalTable: "expenses",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receipts_household_members_HouseholdId_UploadedByMemberId",
                        columns: x => new { x.HouseholdId, x.UploadedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receipts_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_receipts_incomes_HouseholdId_IncomeId",
                        columns: x => new { x.HouseholdId, x.IncomeId },
                        principalSchema: "finance",
                        principalTable: "incomes",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "savings_goal_contributions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavingsGoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    ContributedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_savings_goal_contributions", x => x.Id);
                    table.CheckConstraint("ck_savings_contributions_amount_positive", "\"amount_amount\" > 0");
                    table.ForeignKey(
                        name: "FK_savings_goal_contributions_household_members_HouseholdId_Cr~",
                        columns: x => new { x.HouseholdId, x.CreatedByMemberId },
                        principalSchema: "finance",
                        principalTable: "household_members",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_savings_goal_contributions_households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "finance",
                        principalTable: "households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_savings_goal_contributions_savings_goals_HouseholdId_Saving~",
                        columns: x => new { x.HouseholdId, x.SavingsGoalId },
                        principalSchema: "finance",
                        principalTable: "savings_goals",
                        principalColumns: new[] { "HouseholdId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ocr_results",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExtractedData = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocr_results", x => x.Id);
                    table.CheckConstraint("ck_ocr_results_attempt_positive", "\"AttemptNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_ocr_results_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "finance",
                        principalTable: "receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budgets_HouseholdId_CategoryId_month",
                schema: "finance",
                table: "budgets",
                columns: new[] { "HouseholdId", "CategoryId", "month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budgets_HouseholdId_CreatedByMemberId",
                schema: "finance",
                table: "budgets",
                columns: new[] { "HouseholdId", "CreatedByMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_categories_HouseholdId_Name_Kind",
                schema: "finance",
                table: "categories",
                columns: new[] { "HouseholdId", "Name", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_HouseholdId_ParentCategoryId",
                schema: "finance",
                table: "categories",
                columns: new[] { "HouseholdId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_HouseholdId_CategoryId_OccurredOn",
                schema: "finance",
                table: "expenses",
                columns: new[] { "HouseholdId", "CategoryId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_HouseholdId_CreatedByMemberId_OccurredOn",
                schema: "finance",
                table: "expenses",
                columns: new[] { "HouseholdId", "CreatedByMemberId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_HouseholdId_OccurredOn",
                schema: "finance",
                table: "expenses",
                columns: new[] { "HouseholdId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_household_members_HouseholdId_Status_Role",
                schema: "finance",
                table: "household_members",
                columns: new[] { "HouseholdId", "Status", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_household_members_HouseholdId_UserId",
                schema: "finance",
                table: "household_members",
                columns: new[] { "HouseholdId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_household_members_UserId_Status",
                schema: "finance",
                table: "household_members",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_households_CreatedByUserId",
                schema: "finance",
                table: "households",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_incomes_HouseholdId_CategoryId_OccurredOn",
                schema: "finance",
                table: "incomes",
                columns: new[] { "HouseholdId", "CategoryId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_incomes_HouseholdId_CreatedByMemberId",
                schema: "finance",
                table: "incomes",
                columns: new[] { "HouseholdId", "CreatedByMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_incomes_HouseholdId_OccurredOn",
                schema: "finance",
                table: "incomes",
                columns: new[] { "HouseholdId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ocr_results_ReceiptId_AttemptNumber",
                schema: "finance",
                table: "ocr_results",
                columns: new[] { "ReceiptId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocr_results_Status_StartedAt",
                schema: "finance",
                table: "ocr_results",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_ExpenseId",
                schema: "finance",
                table: "receipts",
                column: "ExpenseId",
                filter: "\"ExpenseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_HouseholdId_ExpenseId",
                schema: "finance",
                table: "receipts",
                columns: new[] { "HouseholdId", "ExpenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_HouseholdId_IncomeId",
                schema: "finance",
                table: "receipts",
                columns: new[] { "HouseholdId", "IncomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_HouseholdId_Status_UploadedAt",
                schema: "finance",
                table: "receipts",
                columns: new[] { "HouseholdId", "Status", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_HouseholdId_UploadedByMemberId",
                schema: "finance",
                table: "receipts",
                columns: new[] { "HouseholdId", "UploadedByMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_IncomeId",
                schema: "finance",
                table: "receipts",
                column: "IncomeId",
                filter: "\"IncomeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_Sha256",
                schema: "finance",
                table: "receipts",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_StorageKey",
                schema: "finance",
                table: "receipts",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_FamilyId_RevokedAt",
                schema: "finance",
                table: "refresh_tokens",
                columns: new[] { "FamilyId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "finance",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAt",
                schema: "finance",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_savings_goal_contributions_HouseholdId_CreatedByMemberId",
                schema: "finance",
                table: "savings_goal_contributions",
                columns: new[] { "HouseholdId", "CreatedByMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_savings_goal_contributions_HouseholdId_SavingsGoalId_Contri~",
                schema: "finance",
                table: "savings_goal_contributions",
                columns: new[] { "HouseholdId", "SavingsGoalId", "ContributedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_savings_goals_HouseholdId_CreatedByMemberId",
                schema: "finance",
                table: "savings_goals",
                columns: new[] { "HouseholdId", "CreatedByMemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_savings_goals_HouseholdId_Status",
                schema: "finance",
                table: "savings_goals",
                columns: new[] { "HouseholdId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "finance",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budgets",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ocr_results",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "savings_goal_contributions",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "receipts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "savings_goals",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "incomes",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "household_members",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "households",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "users",
                schema: "finance");
        }
    }
}
