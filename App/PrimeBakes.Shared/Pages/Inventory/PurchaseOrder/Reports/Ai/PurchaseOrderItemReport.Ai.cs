using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.PurchaseOrder.Reports;

public partial class PurchaseOrderItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE ORDER ITEM REPORT: the raw materials on purchase orders placed with parties (suppliers).
		An order line is "received" when a purchase has been recorded against its order and "pending" when it has not.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then a MATERIAL TABLE (CSV) with the top 100 materials by number of orders.
		CSV columns in the normal view: material, category, unit (unit of measurement), qty (quantity ordered, in that unit), pending_qty, orders, pending_orders, suppliers (number of parties it was ordered from).
		In the SUMMARY view the table is one row per material for the whole period with quantity only, so there is no pending split and no party split.
		Raw materials use different units (kg, litre and so on). Never add or compare quantities of materials that have different units. Rank materials across units by number of orders, and by quantity only within one unit.
		The data only contains materials that were ordered in the period. A material that is missing was not ordered; say "no orders".

		HOW THE BUSINESS WORKS
		- A pending line that is not yet due is not late. A line is "overdue" only when it is pending and its expected delivery date is before today; the data lists these.
		- The data does not say why a line is pending or late, so never assume a cause.
		- Copy material and party names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of different materials, orders and order lines for the period, and how many lines are received and pending (say the split is not available if the data says so); " +
		"(2) the three materials ordered most often, with their orders and quantities in their own units; " +
		"(3) the party with the most orders and how many different materials it supplies; " +
		"(4) the pending materials with the most pending orders, with their pending quantities in their own units; " +
		"(5) one thing that needs attention from this data, such as overdue lines, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which materials are ordered most often?",
		"What is still pending?",
		"Which party supplies the most materials?",
		"Which lines are overdue?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		// Dates are handled as yyyy-MM-dd text so this works whether a model date is DateTime, nullable or DateOnly
		static string Ymd(IFormattable value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
		static DateTime? Day(IFormattable value) =>
			DateTime.TryParseExact(Ymd(value), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

		var all = _transactionOverviews;
		var today = _aiToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}.");
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Orders shown: {(_showPendingOnly ? "pending only" : "received and pending")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per material for the whole period, so there is no pending split and no party split.");
			text.AppendLine($"All materials: materials={all.Count}");
			text.AppendLine("Top materials by quantity within each unit: " + string.Join("; ", all
				.GroupBy(t => t.UnitOfMeasurement)
				.Select(g => $"{Clean(g.Key)}: " + string.Join(", ", g.OrderByDescending(t => t.Quantity).Take(5).Select(t => $"{Clean(t.ItemName)}={N(t.Quantity)}")))));
			text.AppendLine();
			text.AppendLine("MATERIAL TABLE (CSV, whole period)");
			text.AppendLine("material,category,unit,qty");
			foreach (var t in all.OrderBy(t => t.ItemCategoryName).ThenBy(t => t.ItemName).Take(100))
				text.AppendLine(string.Join(',', Clean(t.ItemName), Clean(t.ItemCategoryName), Clean(t.UnitOfMeasurement), N(t.Quantity)));

			return text.ToString();
		}

		var pending = all.Where(t => t.PurchaseId is null).ToList();

		var materials = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Category = g.First().ItemCategoryName,
				Unit = g.First().UnitOfMeasurement,
				Qty = g.Sum(t => t.Quantity),
				PendingQty = g.Where(t => t.PurchaseId is null).Sum(t => t.Quantity),
				Orders = g.Select(t => t.TransactionNo).Distinct().Count(),
				PendingOrders = g.Where(t => t.PurchaseId is null).Select(t => t.TransactionNo).Distinct().Count(),
				Suppliers = g.Select(t => t.PartyName).Distinct().Count()
			})
			.OrderByDescending(x => x.Orders).ThenBy(x => x.Name)
			.ToList();

		text.AppendLine($"All order lines: lines={all.Count}, different materials={materials.Count}, orders={all.Select(t => t.TransactionNo).Distinct().Count()}, " +
			$"received lines={all.Count - pending.Count} ({Pct(all.Count - pending.Count, all.Count)}), pending lines={pending.Count} ({Pct(pending.Count, all.Count)})");

		text.AppendLine("Top 15 materials by number of orders: " + string.Join("; ", materials.Take(15)
			.Select(x => $"{Clean(x.Name)}={x.Orders} orders, {N(x.Qty)} {Clean(x.Unit)} ({x.PendingOrders} pending, {x.Suppliers} suppliers)")));

		text.AppendLine("Top materials by quantity within each unit: " + string.Join("; ", materials
			.GroupBy(x => x.Unit)
			.Select(g => $"{Clean(g.Key)}: " + string.Join(", ", g.OrderByDescending(x => x.Qty).Take(5).Select(x => $"{Clean(x.Name)}={N(x.Qty)}")))));

		text.AppendLine("Materials with the most pending orders: " + string.Join("; ", materials
			.Where(x => x.PendingOrders > 0).OrderByDescending(x => x.PendingOrders).ThenByDescending(x => x.PendingQty).Take(10)
			.Select(x => $"{Clean(x.Name)}={x.PendingOrders} pending orders, {N(x.PendingQty)} {Clean(x.Unit)} pending")));

		text.AppendLine("Categories by number of lines: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Lines = g.Count() })
			.OrderByDescending(x => x.Lines)
			.Select(x => $"{Clean(x.Name)}={x.Lines} ({Pct(x.Lines, all.Count)})")));

		text.AppendLine("By party, most orders first: " + string.Join("; ", all
			.GroupBy(t => t.PartyName)
			.Select(g => new
			{
				Name = g.Key,
				Orders = g.Select(t => t.TransactionNo).Distinct().Count(),
				Materials = g.Select(t => t.ItemName).Distinct().Count(),
				Pending = g.Where(t => t.PurchaseId is null).Select(t => t.TransactionNo).Distinct().Count()
			})
			.OrderByDescending(x => x.Orders).Take(15)
			.Select(x => $"{Clean(x.Name)}={x.Orders} orders / {x.Materials} materials ({x.Pending} pending orders)")));

		var overdue = pending
			.Where(t => Ymd(t.ExpectedDeliveryDate).Length > 0 && string.CompareOrdinal(Ymd(t.ExpectedDeliveryDate), today) < 0)
			.OrderBy(t => Ymd(t.ExpectedDeliveryDate))
			.ToList();

		text.AppendLine($"Overdue pending lines (expected delivery before today): {overdue.Count}");
		text.AppendLine("Most overdue lines: " + string.Join("; ", overdue.Take(10)
			.Select(t => $"{Clean(t.ItemName)} from {Clean(t.PartyName)}: {N(t.Quantity)} {Clean(t.UnitOfMeasurement)}, expected {Ymd(t.ExpectedDeliveryDate)}, {(_aiToday - Day(t.ExpectedDeliveryDate)!.Value).Days} days overdue")));

		text.AppendLine();
		text.AppendLine("MATERIAL TABLE (CSV, top 100 materials by number of orders)");
		text.AppendLine("material,category,unit,qty,pending_qty,orders,pending_orders,suppliers");

		foreach (var x in materials.Take(100))
			text.AppendLine(string.Join(',',
				Clean(x.Name), Clean(x.Category), Clean(x.Unit), N(x.Qty), N(x.PendingQty), x.Orders.ToString(), x.PendingOrders.ToString(), x.Suppliers.ToString()));

		return text.ToString();
	}
}
