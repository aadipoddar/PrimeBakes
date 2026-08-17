using PrimeBakes.Api;
using PrimeBakes.Api.Common;
using PrimeBakes.Data.DataAccess;

var builder = WebApplication.CreateBuilder(args);

SqlDataAccess.SetupConfiguration();

builder.Services.AddServices();

var app = builder.Build();

app.UseServices();

await ApiCachePolicy.RefreshTimeout();

app.Run();
