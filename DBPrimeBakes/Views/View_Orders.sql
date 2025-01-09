CREATE VIEW [dbo].[View_Orders]
	AS
	SELECT
        ot.Id OrderId,
        ut.Name UserName,
        ct.Name CustomerName,
        ot.DateTime OrderDateTime,
        ot.Status
    FROM [Order] ot
    JOIN [User] ut ON ot.UserId = ut.Id
    JOIN [Customer] ct ON ot.CustomerId = ct.Id