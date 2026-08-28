CREATE PROCEDURE [dbo].[Load_WebPushSubscription_By_UserId]
	@UserId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[WebPushSubscription]
	WHERE [UserId] = @UserId;
END
