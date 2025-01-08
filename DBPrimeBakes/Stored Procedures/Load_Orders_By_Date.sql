CREATE PROCEDURE Load_Orders_By_Date
	@FromDate DATETIME,
	@ToDate DATETIME,
	@Status BIT
AS
	SELECT
    ot.Id OrderId,
    ut.Name UserName,
    ct.Name CustomerName,
    ot.DateTime OrderDateTime
FROM [Order] ot
JOIN [User] ut ON ot.UserId = ut.Id
JOIN [Customer] ct ON ot.CustomerId = ct.Id
WHERE ot.DateTime BETWEEN @FromDate AND @ToDate
	AND ot.Status = @Status
RETURN 0