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
