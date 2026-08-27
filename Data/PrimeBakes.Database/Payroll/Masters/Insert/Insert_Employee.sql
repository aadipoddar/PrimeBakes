CREATE PROCEDURE [dbo].[Insert_Employee]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@LocationId INT,
	@DepartmentId INT,
	@DesignationId INT,
	@UserId INT = NULL,
	@DateOfJoining DATE,
	@DateOfLeaving DATE = NULL,
	@DateOfBirth DATE = NULL,
	@Gender VARCHAR(10) = NULL,
	@FatherOrHusbandName VARCHAR(500) = NULL,
	@Phone VARCHAR(20) = NULL,
	@Email VARCHAR(250) = NULL,
	@Address VARCHAR(MAX) = NULL,
	@PAN VARCHAR(10) = NULL,
	@Aadhaar VARCHAR(12) = NULL,
	@PFNumber VARCHAR(50) = NULL,
	@UANNumber VARCHAR(20) = NULL,
	@ESINumber VARCHAR(50) = NULL,
	@BankName VARCHAR(250) = NULL,
	@BankAccountNumber VARCHAR(50) = NULL,
	@IFSC VARCHAR(11) = NULL,
	@PaymentMode VARCHAR(20) = NULL,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Employee]
		(
			[Name],
			[Code],
			[LocationId],
			[DepartmentId],
			[DesignationId],
			[UserId],
			[DateOfJoining],
			[DateOfLeaving],
			[DateOfBirth],
			[Gender],
			[FatherOrHusbandName],
			[Phone],
			[Email],
			[Address],
			[PAN],
			[Aadhaar],
			[PFNumber],
			[UANNumber],
			[ESINumber],
			[BankName],
			[BankAccountNumber],
			[IFSC],
			[PaymentMode],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@LocationId,
			@DepartmentId,
			@DesignationId,
			@UserId,
			@DateOfJoining,
			@DateOfLeaving,
			@DateOfBirth,
			@Gender,
			@FatherOrHusbandName,
			@Phone,
			@Email,
			@Address,
			@PAN,
			@Aadhaar,
			@PFNumber,
			@UANNumber,
			@ESINumber,
			@BankName,
			@BankAccountNumber,
			@IFSC,
			@PaymentMode,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Employee]
		SET [Name] = @Name,
			[Code] = @Code,
			[LocationId] = @LocationId,
			[DepartmentId] = @DepartmentId,
			[DesignationId] = @DesignationId,
			[UserId] = @UserId,
			[DateOfJoining] = @DateOfJoining,
			[DateOfLeaving] = @DateOfLeaving,
			[DateOfBirth] = @DateOfBirth,
			[Gender] = @Gender,
			[FatherOrHusbandName] = @FatherOrHusbandName,
			[Phone] = @Phone,
			[Email] = @Email,
			[Address] = @Address,
			[PAN] = @PAN,
			[Aadhaar] = @Aadhaar,
			[PFNumber] = @PFNumber,
			[UANNumber] = @UANNumber,
			[ESINumber] = @ESINumber,
			[BankName] = @BankName,
			[BankAccountNumber] = @BankAccountNumber,
			[IFSC] = @IFSC,
			[PaymentMode] = @PaymentMode,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
