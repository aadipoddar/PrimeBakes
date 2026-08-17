using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using PrimeBakes.Api.Common;
using PrimeBakes.Data.Operations.User;

using Scalar.AspNetCore;

namespace PrimeBakes.Api;

public static class StartupConfig
{
	private const long _maxRequestBytes = 209715200;

	private const string _landing = """
		<!DOCTYPE html>
		<html lang="en">
		<head>
			<meta charset="utf-8" />
			<title>Prime Bakes API</title>
			<link rel="icon" type="image/png" href="/favicon.png" />
		</head>
		<body style="margin:0;height:100vh;display:grid;place-items:center;background:#111;color:#eee;font-family:system-ui,sans-serif">
			<div style="text-align:center">
				<img src="/favicon.png" alt="Prime Bakes" width="96" height="96" />
				<h1 style="font-size:1.25rem;font-weight:500;letter-spacing:.02em">Prime Bakes API</h1>
			</div>
		</body>
		</html>
		""";

	public static void AddServices(this IServiceCollection services)
	{
		services.AddOpenApi();
		services.AddCors();
		services.AddCarter();
		services.AddOutputCache();

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options => options.TokenValidationParameters = new()
			{
				IssuerSigningKey = AuthData.SigningKey,
				ValidateIssuerSigningKey = true,
				ValidateIssuer = false,
				ValidateAudience = false,
				ClockSkew = TimeSpan.Zero
			});

		services.AddAuthorization(options => options.FallbackPolicy =
			new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

		services.AddExceptionHandler<GlobalExceptionHandler>();
		services.AddProblemDetails();
		services.ConfigureHttpJsonOptions(options => options.SerializerOptions.IncludeFields = true);

		services.AddResponseCompression(options =>
		{
			options.EnableForHttps = true;
			options.MimeTypes = [.. ResponseCompressionDefaults.MimeTypes, "application/json"];
		});

		services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = _maxRequestBytes);
		services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = _maxRequestBytes);
		services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = _maxRequestBytes);
	}

	public static void UseServices(this WebApplication app)
	{
		app.UseExceptionHandler();
		app.UseResponseCompression();

		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi().AllowAnonymous();
			app.MapScalarApiReference(options =>
			{
				options.Title = "Prime Bakes API";
				options.Favicon = "/favicon.png";
			}).AllowAnonymous();
		}

		app.UseHttpsRedirection();
		app.UseStaticFiles();

		app.UseCors(builder => builder
			.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader()
			.WithExposedHeaders("Content-Disposition"));

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseOutputCache();
		app.UseApiCacheEviction();

		app.MapCarter();
		app.MapGet("/", () => Results.Content(_landing, "text/html")).AllowAnonymous();
	}
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
	{
		logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		await context.Response.WriteAsJsonAsync(new { message = exception.Message }, cancellationToken);

		return true;
	}
}
