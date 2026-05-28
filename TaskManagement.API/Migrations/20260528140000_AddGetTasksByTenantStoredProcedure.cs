using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGetTasksByTenantStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE GetTasksByTenant
                    @TenantId INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        Id,
                        Title,
                        Description,
                        IsCompleted,
                        IsDeleted,
                        TenantId,
                        CreatedByUserId
                    FROM Tasks
                    WHERE TenantId = @TenantId
                      AND IsDeleted = 0
                    ORDER BY Id DESC;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GetTasksByTenant;");
        }
    }
}
