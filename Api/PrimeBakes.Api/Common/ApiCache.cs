using Microsoft.AspNetCore.OutputCaching;

using PrimeBakes.Api.Accounts.Masters;
using PrimeBakes.Api.Inventory.Kitchen;
using PrimeBakes.Api.Inventory.RawMaterial;
using PrimeBakes.Api.Inventory.Recipe;
using PrimeBakes.Api.Operations.Analysis;
using PrimeBakes.Api.Operations.Location;
using PrimeBakes.Api.Operations.Settings;
using PrimeBakes.Api.Operations.User;
using PrimeBakes.Api.Payroll.Masters;
using PrimeBakes.Api.Restaurant.Dining;
using PrimeBakes.Api.Store.Customer;
using PrimeBakes.Api.Store.Product;
using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Inventory.Recipe;
using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Data.Operations.Location;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Payroll.Masters;
using PrimeBakes.Data.Store.Customer;
using PrimeBakes.Data.Store.Product;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Api.Common;

public sealed class ApiCachePolicy : IOutputCachePolicy
{
	public const string Tag = "cached";

	public static readonly ApiCachePolicy Instance = new();

	private const int _defaultTimeoutMinutes = 60;
	private static int _timeoutMinutes = _defaultTimeoutMinutes;

	private static readonly HashSet<string> _tables = new(StringComparer.OrdinalIgnoreCase)
	{
		AccountNames.Company,
		AccountNames.Group,
		AccountNames.AccountType,
		AccountNames.StateUT,
		AccountNames.Ledger,
		AccountNames.Voucher,
		AccountNames.FinancialYear,
		OperationNames.Location,
		OperationNames.Settings,
		OperationNames.User,
		InventoryNames.Kitchen,
		InventoryNames.RawMaterial,
		InventoryNames.RawMaterialCategory,
		StoreNames.Product,
		StoreNames.ProductCategory,
		StoreNames.KOTCategory,
		StoreNames.ProductLocation,
		StoreNames.Tax,
		StoreNames.Customer,
		RestaurantNames.DiningArea,
		RestaurantNames.DiningTable,
		PayrollNames.Department,
		PayrollNames.Designation,
		PayrollNames.Employee,
		PayrollNames.SalaryComponent,
		PayrollNames.EmployeeSalaryComponentOverview
	};

	private static readonly HashSet<string> _routes = new(StringComparer.OrdinalIgnoreCase)
	{
		Route(nameof(RecipeEndpoint), nameof(RecipeData.LoadAllRecipes)),
		Route(nameof(AnalysisEndpoint), nameof(AnalysisData.LoadDashboardMonthlyTrend)),
		Route(nameof(AnalysisEndpoint), nameof(AnalysisData.LoadDashboardTopProducts)),
		Route(nameof(AnalysisEndpoint), nameof(AnalysisData.LoadDashboardTopRawMaterials)),
		Route(nameof(SettingsEndpoint), nameof(SettingsData.LoadSettingsByKey)),
		Route(nameof(SettingsEndpoint), nameof(SettingsData.LoadPrimaryCompany)),
		Route(nameof(FinancialYearEndpoint), nameof(FinancialYearData.LoadFinancialYearByDateTime)),
		Route(nameof(LocationEndpoint), nameof(LocationData.LoadLedgerByLocationId)),
		Route(nameof(ProductLocationEndpoint), nameof(ProductLocationData.LoadProductLocationOverviewByProductLocationDate)),
		Route(nameof(CustomerEndpoint), nameof(CustomerData.LoadCustomerByNumber)),
		Route(nameof(EmployeeSalaryComponentEndpoint), nameof(EmployeeSalaryComponentData.LoadEmployeeSalaryComponentOverviewByEmployeeSalaryComponentDate)),
		Route(nameof(EmployeeSalaryComponentEndpoint), nameof(EmployeeSalaryComponentData.LoadEffectiveSalaryComponents))
	};

	private static readonly HashSet<string> _endpoints = new(StringComparer.OrdinalIgnoreCase)
	{
		Helper.SanitizeClassName(nameof(CompanyEndpoint)),
		Helper.SanitizeClassName(nameof(GroupEndpoint)),
		Helper.SanitizeClassName(nameof(AccountTypeEndpoint)),
		Helper.SanitizeClassName(nameof(StateUTEndpoint)),
		Helper.SanitizeClassName(nameof(LedgerEndpoint)),
		Helper.SanitizeClassName(nameof(VoucherEndpoint)),
		Helper.SanitizeClassName(nameof(FinancialYearEndpoint)),
		Helper.SanitizeClassName(nameof(LocationEndpoint)),
		Helper.SanitizeClassName(nameof(SettingsEndpoint)),
		Helper.SanitizeClassName(nameof(UserEndpoint)),
		Helper.SanitizeClassName(nameof(KitchenEndpoint)),
		Helper.SanitizeClassName(nameof(RawMaterialEndpoint)),
		Helper.SanitizeClassName(nameof(RawMaterialCategoryEndpoint)),
		Helper.SanitizeClassName(nameof(RecipeEndpoint)),
		Helper.SanitizeClassName(nameof(ProductEndpoint)),
		Helper.SanitizeClassName(nameof(ProductCategoryEndpoint)),
		Helper.SanitizeClassName(nameof(KOTCategoryEndpoint)),
		Helper.SanitizeClassName(nameof(ProductLocationEndpoint)),
		Helper.SanitizeClassName(nameof(TaxEndpoint)),
		Helper.SanitizeClassName(nameof(CustomerEndpoint)),
		Helper.SanitizeClassName(nameof(DiningAreaEndpoint)),
		Helper.SanitizeClassName(nameof(DiningTableEndpoint)),
		Helper.SanitizeClassName(nameof(DepartmentEndpoint)),
		Helper.SanitizeClassName(nameof(DesignationEndpoint)),
		Helper.SanitizeClassName(nameof(EmployeeEndpoint)),
		Helper.SanitizeClassName(nameof(SalaryComponentEndpoint)),
		Helper.SanitizeClassName(nameof(EmployeeSalaryComponentEndpoint))
	};

	private static string Route(string endpointClass, string function) =>
		Helper.MakeRouteFromEndpointFunction(Helper.SanitizeClassName(endpointClass), function);

	ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
	{
		var request = context.HttpContext.Request;

		var cacheable = HttpMethods.IsGet(request.Method)
			&& (_routes.Contains(request.Path.Value?.Trim('/') ?? string.Empty)
				|| (request.Query.TryGetValue("TableName", out var tableName) && _tables.Contains(tableName.ToString())));

		context.EnableOutputCaching = cacheable;
		context.AllowCacheLookup = cacheable;
		context.AllowCacheStorage = cacheable;
		context.AllowLocking = true;

		if (cacheable)
		{
			context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(_timeoutMinutes);
			context.CacheVaryByRules.QueryKeys = "*";
			context.Tags.Add(Tag);
		}

		return ValueTask.CompletedTask;
	}

	ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
		ValueTask.CompletedTask;

	ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
	{
		if (context.HttpContext.Response.StatusCode != StatusCodes.Status200OK)
			context.AllowCacheStorage = false;

		return ValueTask.CompletedTask;
	}

	public static async Task RefreshTimeout()
	{
		try
		{
			var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.CacheTimeoutMinutes);

			_timeoutMinutes = setting is not null && int.TryParse(setting.Value, out var minutes) && minutes > 0
				? minutes
				: _defaultTimeoutMinutes;
		}
		catch
		{
		}
	}

	public static bool EvictsCache(HttpRequest request) =>
		HttpMethods.IsPost(request.Method)
			&& request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() is string segment
			&& _endpoints.Contains(segment);
}

public static class ApiCacheExtensions
{
	public static void UseApiCacheEviction(this WebApplication app) =>
		app.Use(async (context, next) =>
		{
			await next();

			if (context.Response.StatusCode >= StatusCodes.Status400BadRequest || !ApiCachePolicy.EvictsCache(context.Request))
				return;

			await context.RequestServices.GetRequiredService<IOutputCacheStore>()
				.EvictByTagAsync(ApiCachePolicy.Tag, CancellationToken.None);

			await ApiCachePolicy.RefreshTimeout();
		});
}
