CREATE PROCEDURE [dbo].[Load_Terminal_By_MachineId]
	@MachineId VARCHAR(100)
AS
BEGIN

	SELECT * FROM [Terminal]
	WHERE [MachineId] = @MachineId

END