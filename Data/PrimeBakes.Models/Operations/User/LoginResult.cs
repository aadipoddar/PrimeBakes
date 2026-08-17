namespace PrimeBakes.Models.Operations.User;

public sealed record LoginResult(UserModel User, string Token);
