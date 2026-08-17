using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

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

	public static async Task<LoginResult> Login(int Passcode)
	{
		var user = await UserData.LoadUserByPasscode(Passcode);

		return user is null || !user.Status ? null : new LoginResult(user, CreateToken(user));
	}

	private static string CreateToken(UserModel user) =>
		new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())]),
			Expires = DateTime.UtcNow.AddDays(_tokenValidDays),
			SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
		});
}
