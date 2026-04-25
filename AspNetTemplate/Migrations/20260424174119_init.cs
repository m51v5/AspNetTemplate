using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetTemplate.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jti = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Extension = table.Column<string>(type: "text", nullable: false),
                    FolderPath = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_access_tokens_user_active",
                table: "AccessTokens",
                columns: new[] { "UserId", "UserRole" },
                filter: "\"IsRevoked\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_CreatedAt",
                table: "AccessTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_Jti",
                table: "AccessTokens",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_CreatedAt",
                table: "Files",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FileId",
                table: "Users",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsAdmin",
                table: "Users",
                column: "IsAdmin",
                unique: true,
                filter: "\"IsAdmin\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.Sql("CREATE FUNCTION \"LC_TRIGGER_AFTER_DELETE_USER\"() RETURNS trigger as $LC_TRIGGER_AFTER_DELETE_USER$\r\nBEGIN\r\n  \r\n  IF OLD.\"FileId\" IS NOT NULL THEN \r\n    UPDATE \"Files\"\r\n    SET \"UsageCount\" = \"Files\".\"UsageCount\" - 1\r\n    WHERE OLD.\"FileId\" = \"Files\".\"Id\";\r\n  END IF;\r\nRETURN OLD;\r\nEND;\r\n$LC_TRIGGER_AFTER_DELETE_USER$ LANGUAGE plpgsql;\r\nCREATE TRIGGER LC_TRIGGER_AFTER_DELETE_USER AFTER DELETE\r\nON \"Users\"\r\nFOR EACH ROW EXECUTE PROCEDURE \"LC_TRIGGER_AFTER_DELETE_USER\"();");

            migrationBuilder.Sql("CREATE FUNCTION \"LC_TRIGGER_AFTER_INSERT_USER\"() RETURNS trigger as $LC_TRIGGER_AFTER_INSERT_USER$\r\nBEGIN\r\n  \r\n  IF NEW.\"FileId\" IS NOT NULL THEN \r\n    UPDATE \"Files\"\r\n    SET \"UsageCount\" = \"Files\".\"UsageCount\" + 1\r\n    WHERE NEW.\"FileId\" = \"Files\".\"Id\";\r\n  END IF;\r\nRETURN NEW;\r\nEND;\r\n$LC_TRIGGER_AFTER_INSERT_USER$ LANGUAGE plpgsql;\r\nCREATE TRIGGER LC_TRIGGER_AFTER_INSERT_USER AFTER INSERT\r\nON \"Users\"\r\nFOR EACH ROW EXECUTE PROCEDURE \"LC_TRIGGER_AFTER_INSERT_USER\"();");

            migrationBuilder.Sql("CREATE FUNCTION \"LC_TRIGGER_AFTER_UPDATE_USER\"() RETURNS trigger as $LC_TRIGGER_AFTER_UPDATE_USER$\r\nBEGIN\r\n  \r\n  IF OLD.\"FileId\" IS NOT NULL AND OLD.\"FileId\" <> NEW.\"FileId\" THEN \r\n    UPDATE \"Files\"\r\n    SET \"UsageCount\" = \"Files\".\"UsageCount\" - 1\r\n    WHERE OLD.\"FileId\" = \"Files\".\"Id\";\r\n  END IF;\r\n  \r\n  IF NEW.\"FileId\" IS NOT NULL AND OLD.\"FileId\" <> NEW.\"FileId\" THEN \r\n    UPDATE \"Files\"\r\n    SET \"UsageCount\" = \"Files\".\"UsageCount\" + 1\r\n    WHERE NEW.\"FileId\" = \"Files\".\"Id\";\r\n  END IF;\r\nRETURN NEW;\r\nEND;\r\n$LC_TRIGGER_AFTER_UPDATE_USER$ LANGUAGE plpgsql;\r\nCREATE TRIGGER LC_TRIGGER_AFTER_UPDATE_USER AFTER UPDATE\r\nON \"Users\"\r\nFOR EACH ROW EXECUTE PROCEDURE \"LC_TRIGGER_AFTER_UPDATE_USER\"();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION \"LC_TRIGGER_AFTER_DELETE_USER\"() CASCADE;");

            migrationBuilder.Sql("DROP FUNCTION \"LC_TRIGGER_AFTER_INSERT_USER\"() CASCADE;");

            migrationBuilder.Sql("DROP FUNCTION \"LC_TRIGGER_AFTER_UPDATE_USER\"() CASCADE;");

            migrationBuilder.DropTable(
                name: "AccessTokens");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
