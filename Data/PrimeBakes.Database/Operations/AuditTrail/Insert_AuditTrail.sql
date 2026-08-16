CREATE PROCEDURE [dbo].[Insert_AuditTrail]
	@Id INT OUTPUT,
	@Action VARCHAR(MAX),
	@TableName VARCHAR(MAX),
	@RecordNo VARCHAR(MAX),
	@RecordValue VARCHAR(MAX),
	@CreatedBy INT,
	@CreatedByName VARCHAR(MAX),
	@TransactionDateTime DATETIME,
	@CreatedFromPlatform VARCHAR(MAX)
AS
BEGIN
	INSERT INTO [dbo].[AuditTrail]
	(
		[Action],
		[TableName],
		[RecordNo],
		[RecordValue],
		[CreatedBy],
		[CreatedByName],
		[CreatedFromPlatform]
	)
	VALUES
	(
		@Action,
		@TableName,
		@RecordNo,
		@RecordValue,
		@CreatedBy,
		@CreatedByName,
		@CreatedFromPlatform
	);

	SET @Id = SCOPE_IDENTITY();

	SELECT @Id AS Id;
END
