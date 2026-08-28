CREATE VIEW [dbo].[AuditTrail_Overview]
	AS
SELECT
	[a].[Id],
	[a].[Action],
	[a].[TableName],
	[a].[RecordNo],
	[a].[RecordValue],
	[a].[CreatedBy],
	[a].[CreatedByName],
	[a].[TransactionDateTime],
	[a].[CreatedFormFactor],
	[a].[CreatedPlatform],
	[a].[CreatedLatitude],
	[a].[CreatedLongitude],

	CASE WHEN [a].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([a].[CreatedLatitude], [a].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset

FROM
	[dbo].[AuditTrail] a

LEFT JOIN
	[dbo].[User] AS u ON a.CreatedBy = u.Id
LEFT JOIN
	[dbo].[Location] AS ul ON u.LocationId = ul.Id
