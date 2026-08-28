using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;

using System.Security.Claims;
using System.Text;

namespace PrimeBakes.Data.Operations.User;

public static class AuthData
{
	private const int _tokenValidDays = 30;

	public static SymmetricSecurityKey SigningKey { get; } = new(Encoding.UTF8.GetBytes(
		string.IsNullOrWhiteSpace(Secrets.JwtKey)
			? throw new InvalidOperationException("Secrets.JwtKey is not set.")
			: Secrets.JwtKey));

	public static async Task<LoginResult> Login(int Passcode, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		var user = await UserData.LoadUserByPasscode(Passcode);

		if (user is null || !user.Status)
			return null;

		await UserData.UpdateLastLoginTime(user, await CommonData.LoadCurrentDateTime());

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Login.ToString(),
			TableName = OperationNames.User,
			RecordNo = user.Name,
			CreatedBy = user.Id,
			CreatedFormFactor = formFactor,
			CreatedPlatform = platform,
			CreatedLatitude = latitude,
			CreatedLongitude = longitude
		});

		return new LoginResult(user, CreateToken(user));
	}

	private static string CreateToken(UserModel user) =>
		new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())]),
			Expires = DateTime.UtcNow.AddDays(_tokenValidDays),
			SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
		});
}
