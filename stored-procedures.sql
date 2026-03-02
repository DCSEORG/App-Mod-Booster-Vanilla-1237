-- stored-procedures.sql
-- Create or alter all stored procedures needed by the Expense Management App

CREATE OR ALTER PROCEDURE dbo.sp_GetExpenses
    @StatusFilter NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ExpenseId, e.UserId, u.UserName, e.CategoryId, c.CategoryName, 
           e.StatusId, s.StatusName, e.AmountMinor, e.Currency, e.ExpenseDate,
           e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy,
           ru.UserName AS ReviewedByName, e.ReviewedAt, e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users ru ON e.ReviewedBy = ru.UserId
    WHERE (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
    ORDER BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.ExpenseId, e.UserId, u.UserName, e.CategoryId, c.CategoryName,
           e.StatusId, s.StatusName, e.AmountMinor, e.Currency, e.ExpenseDate,
           e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy,
           ru.UserName AS ReviewedByName, e.ReviewedAt, e.CreatedAt
    FROM dbo.Expenses e
    JOIN dbo.Users u ON e.UserId = u.UserId
    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users ru ON e.ReviewedBy = ru.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @Currency NVARCHAR(3) = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @StatusId INT = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateExpenseStatus
    @ExpenseId INT,
    @StatusId INT,
    @ReviewedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = CASE WHEN @ReviewedBy IS NOT NULL THEN SYSUTCDATETIME() ELSE ReviewedAt END,
        SubmittedAt = CASE WHEN (SELECT StatusName FROM dbo.ExpenseStatus WHERE StatusId = @StatusId) = 'Submitted' 
                          THEN SYSUTCDATETIME() ELSE SubmittedAt END
    WHERE ExpenseId = @ExpenseId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, u.ManagerId, u.IsActive
    FROM dbo.Users u
    JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO
