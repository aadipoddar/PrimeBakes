CREATE PROCEDURE [dbo].[Delete_WebPushSubscription_By_Endpoint]
	@Endpoint VARCHAR(500)
AS
BEGIN
	DELETE FROM [dbo].[WebPushSubscription]
	WHERE [Endpoint] = @Endpoint;
END
