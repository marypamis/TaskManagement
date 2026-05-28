using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class EnsureTaskIsCompletedDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Optional server-side default for existing rows/tools; inserts still send explicit values from the API.
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                    WHERE dc.parent_object_id = OBJECT_ID('Tasks') AND c.name = 'IsCompleted')
                BEGIN
                    ALTER TABLE Tasks ADD CONSTRAINT DF_Tasks_IsCompleted DEFAULT 0 FOR IsCompleted;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Tasks_IsCompleted')
                BEGIN
                    ALTER TABLE Tasks DROP CONSTRAINT DF_Tasks_IsCompleted;
                END
                """);
        }
    }
}
