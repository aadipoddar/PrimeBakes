CREATE PROCEDURE Load_Orders_By_Date_Status
	@FromDate DATE,
	@ToDate DATE,
	@Status BIT
AS
BEGIN

	SELECT 
		OrderId,
		UserName,
		CustomerName,
		OrderDateTime
	FROM View_Orders vo
	WHERE vo.OrderDateTime BETWEEN @FromDate AND @ToDate
		AND vo.Status = @Status

END