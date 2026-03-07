CREATE PROCEDURE [dbo].[Load_Sale_Summary_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	Select * from Sale
END