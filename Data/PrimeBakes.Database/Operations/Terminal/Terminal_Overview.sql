CREATE VIEW [dbo].[Terminal_Overview]
	AS
SELECT
	[t].[Id],
	[t].[TerminalNo],
	[t].[MachineName],
	[t].[MachineId],
	[t].[UserId],
	[u].[Name] AS UserName,
	[t].[LastSyncedAt]

FROM
	[dbo].[Terminal] t

LEFT JOIN
	[dbo].[User] AS u ON t.UserId = u.Id
