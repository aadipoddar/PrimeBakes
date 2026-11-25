using PrimeBakes.Models.Inventory.Stock;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class OutletDetailReport
{
	private const int MainOutletLocationId = 1;

	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the OUTLET DETAIL REPORT: everything for ONE outlet (the one named in REPORT DATA) over the selected period.
		REPORT DATA has the outlet and period, then PRECOMPUTED TOTALS, then lists, then a DAY TABLE (CSV; the rows are months when the period is longer than 62 days), an ITEM TABLE (CSV) with the top 50 items by units sold and a STOCK TABLE (CSV) with the top 30 items by closing stock value.
		DAY TABLE columns: period, transactions, units, sale, sale_return, net_sale, cash, upi, card, credit, discount.
		ITEM TABLE columns: item, category, units, net (net value of the outlet's own sales).
		STOCK TABLE columns: item, category, opening, in, out, closing, closing_value, days_on_hand.
		Sections of the totals:
		- "Outlet sales": the outlet's own sales to customers (sales, stock transfers made from it, and restaurant bills), less customer returns.
		- "Supply from main outlet": stock the main outlet supplied to this outlet (sales to its ledger plus stock transfers to it), with the discount given on that supply.
		- "Returned to main outlet": stock this outlet sent back to the main outlet.
		- "Orders": what the outlet ordered, with ordered against supplied quantities per item.
		- "Restaurant bills" and "Accounts" (voucher entries on the outlet's ledger, with debit and credit totals).
		- "Stock": this outlet's stock for the period. The summary part has opening, in, out and closing per item; the movement part lists individual stock movements and stock count adjustments.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It buys raw material, makes finished products in the kitchen and sells them to the other outlets on credit. That is internal supply, not customer sales. If this report is for the main outlet, its sales are mostly that supply, so its credit sales and lack of cash, card or UPI are normal and not a concern.
		- Supply from the main outlet appears here as "Supply from main outlet" and as sales in the main outlet's own report. Never add supply to outlet sales.
		- Other outlets sell to customers for cash, UPI or card. Coco outlets are company owned and managed, Fofo outlets are franchise owned, and Other is a different kind of outlet.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So an outlet or a day with low or no recorded sales is NOT necessarily quiet or weak. Never call it inactive or underperforming; say "recorded sales" instead.
		- Supply is bought stock, not the cost of goods sold. This report has no profit figure, so never present sales minus supply as profit.
		- Each outlet has a configured supply discount. The data gives it next to the discount actually given on supply; report both and let the numbers speak, without calling it a violation.
		- Ordered and supplied quantities cover the same dates, but supply can lag, so an order near the end of the period may be supplied after it. A shortfall is a gap in recorded quantities, not proof that stock was withheld; never assume a cause.
		- Returns to the main outlet are either damaged stock or unsold stock; the data does not say which, so call them "returns" and never assume a cause.
		- Stock: at an outlet, supply from the main outlet adds stock, and its customer sales, restaurant bills and stock transfers take stock out. Items are counted in different units (kg, litres, pieces), so quantities of different items must never be added together; compare items by value and quote quantities only per item.
		- Stock values are quantity multiplied by a rate (the item's current rate for closing_value). They are for comparing items, not an accounting valuation.
		- Negative closing stock means more went out than was recorded coming in, which usually points to missing supply entries or to timing. Report it as "negative closing stock" and do not guess a cause.
		- A stock adjustment is a count correction: the counted quantity minus what the system showed. Negative means fewer were found than the system expected; positive means more. Do not say why; the data does not say.
		- days_on_hand is closing stock divided by average daily consumption over the period; 0 means there was no consumption to measure it against, not that stock is out.
		- Quote the precomputed totals and lists instead of adding up rows yourself.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the outlet's net sale, transactions and units for the period, with the average bill; " +
		"(2) the top three items by units, with their units; " +
		"(3) the highest and lowest periods among those with recorded sales, with their amounts, and the cash, UPI and card split as percentages; " +
		"(4) stock supplied from the main outlet and returned to it, with the supply discount given against the configured discount, and the largest gap between ordered and supplied; " +
		"(5) the outlet's closing stock value, how many items have negative closing stock, and the net value of stock count adjustments; if anything else needs attention, add it to this line, otherwise say nothing else needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which items sell the most here?",
		"How much stock came from the main outlet?",
		"Compare ordered and supplied items",
		"Which items have negative stock?"
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

	// Text version of what is on screen for the selected outlet (the lists already have the location and date filters applied).
	// Deleted transactions are always left out here, whatever the Show Deleted toggle says.
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string N(IConvertible value, string format = "0") => D(value).ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";
		static string Join(IEnumerable<string> parts) => parts.Any() ? string.Join("; ", parts) : "none";
		static bool IsAdjustment(string type) => string.Equals(type, "Adjustment", StringComparison.OrdinalIgnoreCase);

		if (_selectedLocation is null)
			return string.Empty;

		var location = _selectedLocation;
		var locationId = location.Id;
		var isMain = locationId == MainOutletLocationId;
		var category = location.COCO ? "Coco" : location.FOFO ? "Fofo" : "Other";

		var from = _fromDate.Date;
		var to = _toDate.Date;
		var days = (to - from).Days + 1;
		var monthly = days > 62;

		DateTime Key(DateTime date) => monthly ? new DateTime(date.Year, date.Month, 1) : date.Date;
		string Label(DateTime key) => monthly ? key.ToString("MMM-yyyy", CultureInfo.InvariantCulture) : key.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

		// Active records only
		var sales = _sales.Where(s => s.Status).ToList();
		var saleItems = _saleItems.Where(s => s.MasterStatus).ToList();
		var saleReturns = _saleReturns.Where(s => s.Status).ToList();
		var saleReturnItems = _saleReturnItems.Where(s => s.MasterStatus).ToList();
		var transfersOut = _stockTransfers.Where(t => t.Status && t.LocationId == locationId).ToList();
		var transferItemsOut = _stockTransferItems.Where(t => t.MasterStatus && t.LocationId == locationId).ToList();
		var transfersIn = _stockTransfers.Where(t => t.Status && t.ToLocationId == locationId).ToList();
		var transferItemsIn = _stockTransferItems.Where(t => t.MasterStatus && t.ToLocationId == locationId).ToList();
		var bills = _bills.Where(b => b.Status).ToList();
		var billItems = _billItems.Where(b => b.MasterStatus).ToList();
		var purchases = _outletPurchases.Where(s => s.Status).ToList();
		var purchaseItems = _outletPurchaseItems.Where(s => s.MasterStatus).ToList();
		var purchaseReturns = _outletPurchaseReturns.Where(s => s.Status).ToList();
		var purchaseReturnItems = _outletPurchaseReturnItems.Where(s => s.MasterStatus).ToList();
		var orders = _orders.Where(o => o.Status).ToList();
		var orderItems = _orderItems.Where(o => o.MasterStatus).ToList();
		var ledgerEntries = _ledgerEntries.Where(l => l.MasterStatus).ToList();

		var text = new StringBuilder();
		text.AppendLine($"Outlet: {Clean(location.Name)} ({category})" + (isMain ? " - this is the MAIN OUTLET (head office), so its sales are mostly internal supply to other outlets" : string.Empty));
		text.AppendLine($"Period: {from:dd-MMM-yyyy} to {to:dd-MMM-yyyy} ({days} days)");
		if (to >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine("Deleted transactions are excluded." +
			$" Deleted in the period at this outlet: sales={_allSales.Count(s => !s.Status && s.LocationId == locationId)}, bills={_allBills.Count(b => !b.Status && b.LocationId == locationId)}.");

		// ---------- Outlet sales (sales + stock transfers out + restaurant bills, less returns) ----------
		var saleRows = sales.Select(s => (Date: s.TransactionDateTime, Count: 1, Total: D(s.TotalAmount), Units: D(s.TotalQuantity), Cash: D(s.Cash), Upi: D(s.UPI), Card: D(s.Card), Credit: D(s.Credit), Discount: D(s.DiscountAmount) + D(s.ItemDiscountAmount)))
			.Concat(transfersOut.Select(s => (Date: s.TransactionDateTime, Count: 1, Total: D(s.TotalAmount), Units: D(s.TotalQuantity), Cash: D(s.Cash), Upi: D(s.UPI), Card: D(s.Card), Credit: D(s.Credit), Discount: D(s.DiscountAmount) + D(s.ItemDiscountAmount))))
			.Concat(bills.Select(s => (Date: s.TransactionDateTime, Count: 1, Total: D(s.TotalAmount), Units: D(s.TotalQuantity), Cash: D(s.Cash), Upi: D(s.UPI), Card: D(s.Card), Credit: D(s.Credit), Discount: D(s.DiscountAmount) + D(s.ItemDiscountAmount))))
			.ToList();

		var saleTotal = saleRows.Sum(r => r.Total);
		var returnTotal = D(saleReturns.Sum(r => r.TotalAmount));
		var netSale = saleTotal - returnTotal;
		var transactions = saleRows.Sum(r => r.Count);
		var units = saleRows.Sum(r => r.Units);
		var cash = saleRows.Sum(r => r.Cash);
		var upi = saleRows.Sum(r => r.Upi);
		var card = saleRows.Sum(r => r.Card);
		var credit = saleRows.Sum(r => r.Credit);
		var paid = cash + upi + card + credit;

		// Per day (or per month for long periods)
		var byPeriod = saleRows.ToLookup(r => Key(r.Date));
		var returnsByPeriod = saleReturns.GroupBy(r => Key(r.TransactionDateTime)).ToDictionary(g => g.Key, g => D(g.Sum(r => r.TotalAmount)));
		var series = byPeriod.Select(g => g.Key).Concat(returnsByPeriod.Keys).Distinct().OrderBy(k => k)
			.Select(k =>
			{
				var rows = byPeriod[k].ToList();
				var returns = returnsByPeriod.GetValueOrDefault(k);
				var total = rows.Sum(r => r.Total);
				return new
				{
					Key = k,
					Count = rows.Sum(r => r.Count),
					Units = rows.Sum(r => r.Units),
					Total = total,
					Returns = returns,
					Net = total - returns,
					Cash = rows.Sum(r => r.Cash),
					Upi = rows.Sum(r => r.Upi),
					Card = rows.Sum(r => r.Card),
					Credit = rows.Sum(r => r.Credit),
					Discount = rows.Sum(r => r.Discount)
				};
			})
			.ToList();

		var recorded = series.Where(x => x.Count > 0).ToList();
		var grain = monthly ? "months" : "days";

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Outlet sales (sales + stock transfers made from this outlet + restaurant bills, less customer returns): sale={N(saleTotal)}, sale_return={N(returnTotal)}, net_sale={N(netSale)}, " +
			$"transactions={transactions}, units={N(units, "0.##")}, avg_bill={(transactions == 0 ? "0" : N(netSale / transactions))}, discount_given={N(saleRows.Sum(r => r.Discount))}");
		text.AppendLine($"Made up of: sales={sales.Count} bills / {N(sales.Sum(s => s.TotalAmount))}; stock transfers made={transfersOut.Count} / {N(transfersOut.Sum(s => s.TotalAmount))}; restaurant bills={bills.Count} / {N(bills.Sum(s => s.TotalAmount))}; customer returns={saleReturns.Count} / {N(returnTotal)}");
		text.AppendLine($"Payment split of outlet sales: cash={N(cash)} ({Pct(cash, paid)}), upi={N(upi)} ({Pct(upi, paid)}), card={N(card)} ({Pct(card, paid)}), credit={N(credit)} ({Pct(credit, paid)})");

		if (recorded.Count > 0)
		{
			var best = recorded.OrderByDescending(x => x.Net).First();
			var worst = recorded.OrderBy(x => x.Net).First();
			text.AppendLine($"Recorded sales in {recorded.Count} of {series.Count} {grain} listed. Highest period: {Label(best.Key)}={N(best.Net)}. Lowest period with recorded sales: {Label(worst.Key)}={N(worst.Net)}. Average per period with recorded sales: {N(recorded.Sum(x => x.Net) / recorded.Count)}");
		}
		else
			text.AppendLine("No recorded outlet sales in the period.");

		// ---------- Supply from / returns to the main outlet ----------
		var supplyBase = D(purchases.Sum(s => s.BaseTotal));
		var supplyDiscount = D(purchases.Sum(s => s.DiscountAmount)) + D(purchases.Sum(s => s.ItemDiscountAmount));
		var supplyTotal = D(purchases.Sum(s => s.TotalAmount)) + D(transfersIn.Sum(s => s.TotalAmount));
		var returnedTotal = D(purchaseReturns.Sum(s => s.TotalAmount));

		text.AppendLine();
		text.AppendLine(isMain
			? "Supply from main outlet: not applicable, this is the main outlet."
			: $"Supply from main outlet (sales to this outlet's ledger + stock transfers to it): total={N(supplyTotal)}, units={N(purchases.Sum(s => s.TotalQuantity) + transfersIn.Sum(s => s.TotalQuantity), "0.##")}, " +
			  $"sales={purchases.Count} / {N(purchases.Sum(s => s.TotalAmount))}, stock transfers={transfersIn.Count} / {N(transfersIn.Sum(s => s.TotalAmount))}");
		if (!isMain)
		{
			text.AppendLine($"Supply discount: configured for this outlet={N(location.Discount, "0.##")}%, given on supply sales={N(supplyDiscount)} ({Pct(supplyDiscount, supplyBase)} of base value {N(supplyBase)}). Stock transfers carry no discount here.");
			text.AppendLine($"Returned to main outlet: {purchaseReturns.Count} returns, total={N(returnedTotal)}, units={N(purchaseReturns.Sum(s => s.TotalQuantity), "0.##")} ({Pct(returnedTotal, supplyTotal)} of supply value)");
		}

		// ---------- Orders: ordered vs supplied per item ----------
		var suppliedByItem = purchaseItems.Select(i => (Name: i.ItemName ?? string.Empty, Qty: D(i.Quantity)))
			.Concat(transferItemsIn.Select(i => (Name: i.ItemName ?? string.Empty, Qty: D(i.Quantity))))
			.GroupBy(x => x.Name)
			.ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

		var orderedByItem = orderItems
			.GroupBy(i => i.ItemName ?? string.Empty)
			.Select(g => new { Name = g.Key, Ordered = D(g.Sum(x => x.Quantity)) })
			.Select(x => new { x.Name, x.Ordered, Supplied = suppliedByItem.GetValueOrDefault(x.Name) })
			.OrderByDescending(x => x.Ordered)
			.ToList();

		text.AppendLine();
		text.AppendLine($"Orders: {orders.Count} orders, {N(orders.Sum(o => o.TotalQuantity), "0.##")} units ordered, {orders.Count(o => !string.IsNullOrWhiteSpace(o.SaleTransactionNo))} of them already linked to a sale");
		text.AppendLine("Ordered against supplied, top 20 items by quantity ordered: " + Join(orderedByItem.Take(20)
			.Select(x => $"{Clean(x.Name)}: ordered {N(x.Ordered, "0.##")}, supplied {N(x.Supplied, "0.##")}")));
		text.AppendLine("Largest shortfalls (ordered more than supplied): " + Join(orderedByItem
			.Where(x => x.Ordered > x.Supplied).OrderByDescending(x => x.Ordered - x.Supplied).Take(5)
			.Select(x => $"{Clean(x.Name)}: short by {N(x.Ordered - x.Supplied, "0.##")}")));

		// ---------- Restaurant bills ----------
		text.AppendLine();
		text.AppendLine($"Restaurant bills: {bills.Count} bills, total={N(bills.Sum(b => b.TotalAmount))}, people={N(bills.Sum(b => b.TotalPeople))}, service charge={N(bills.Sum(b => b.ServiceChargeAmount))}, still running={bills.Count(b => b.Running)}");
		text.AppendLine("Restaurant bills by dining area: " + Join(bills
			.GroupBy(b => b.DiningAreaName ?? "Unknown")
			.Select(g => new { Name = g.Key, Count = g.Count(), Total = D(g.Sum(b => b.TotalAmount)) })
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}: {x.Count} bills / {N(x.Total)}")));

		// ---------- Accounts ----------
		text.AppendLine();
		text.AppendLine($"Accounts (voucher entries on this outlet's ledger): entries={ledgerEntries.Count}, debit={N(ledgerEntries.Sum(l => l.Debit))}, credit={N(ledgerEntries.Sum(l => l.Credit))}");
		text.AppendLine("Accounts by voucher type: " + Join(ledgerEntries
			.GroupBy(l => l.VoucherName ?? "Unknown")
			.Select(g => new { Name = g.Key, Count = g.Count(), Debit = D(g.Sum(l => l.Debit)), Credit = D(g.Sum(l => l.Credit)) })
			.OrderByDescending(x => x.Debit + x.Credit)
			.Select(x => $"{Clean(x.Name)}: {x.Count} entries, debit={N(x.Debit)}, credit={N(x.Credit)}")));
		text.AppendLine($"Products on this outlet's rate list: {_productLocations.Count}");

		// ---------- Items sold ----------
		var items = saleItems.Select(i => (Name: i.ItemName, Category: i.ItemCategoryName ?? "Uncategorised", Qty: D(i.Quantity), Net: D(i.NetTotal)))
			.Concat(transferItemsOut.Select(i => (Name: i.ItemName, Category: i.ItemCategoryName ?? "Uncategorised", Qty: D(i.Quantity), Net: D(i.NetTotal))))
			.Concat(billItems.Select(i => (Name: i.ItemName, Category: i.ItemCategoryName ?? "Uncategorised", Qty: D(i.Quantity), Net: D(i.NetTotal))))
			.GroupBy(r => r.Name)
			.Select(g => new { Name = g.Key, Category = g.First().Category, Units = g.Sum(r => r.Qty), Net = g.Sum(r => r.Net) })
			.OrderByDescending(x => x.Units).ThenBy(x => x.Name)
			.ToList();

		var itemNet = items.Sum(x => x.Net);

		text.AppendLine();
		text.AppendLine($"Items sold by this outlet: {items.Count} different items, {N(items.Sum(x => x.Units), "0.##")} units, net value {N(itemNet)} (item level, before bill discount and round off, so it can differ slightly from net_sale)");
		text.AppendLine("Top 15 items by units: " + Join(items.Take(15).Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units / {N(x.Net)}")));
		text.AppendLine("Top 10 items by net value: " + Join(items.OrderByDescending(x => x.Net).Take(10).Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
		text.AppendLine("Categories by net value: " + Join(items
			.GroupBy(x => x.Category)
			.Select(g => new { Name = g.Key, Units = g.Sum(x => x.Units), Net = g.Sum(x => x.Net) })
			.OrderByDescending(x => x.Net).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} ({Pct(x.Net, itemNet)}) / {N(x.Units, "0.##")} units")));
		text.AppendLine("Top 10 customer returns by value: " + Join(saleReturnItems
			.GroupBy(i => i.ItemName)
			.Select(g => new { Name = g.Key, Units = D(g.Sum(x => x.Quantity)), Net = D(g.Sum(x => x.NetTotal)) })
			.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
		if (!isMain)
		{
			text.AppendLine("Top 10 items supplied by the main outlet (by value): " + Join(purchaseItems
				.GroupBy(i => i.ItemName)
				.Select(g => new { Name = g.Key, Units = D(g.Sum(x => x.Quantity)), Net = D(g.Sum(x => x.NetTotal)) })
				.OrderByDescending(x => x.Net).Take(10)
				.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
			text.AppendLine("Top 10 items returned to the main outlet (by value): " + Join(purchaseReturnItems
				.GroupBy(i => i.ItemName)
				.Select(g => new { Name = g.Key, Units = D(g.Sum(x => x.Quantity)), Net = D(g.Sum(x => x.NetTotal)) })
				.OrderByDescending(x => x.Net).Take(10)
				.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
		}

		// ---------- Stock (Stock Summary and Stock Detail tabs) ----------
		var stock = _stockSummary ?? [];
		var movements = (_stockDetails ?? [])
			.Select(d => (Date: d.TransactionDateTime, Type: string.IsNullOrWhiteSpace(d.Type) ? "Unknown" : d.Type, TransactionNo: d.TransactionNo ?? string.Empty, Item: d.ProductName ?? string.Empty, Qty: D(d.Quantity), Rate: D(d.NetRate), Value: D(d.Total)))
			.ToList();

		text.AppendLine();
		if (stock.Count == 0 && movements.Count == 0)
			text.AppendLine("Stock: no stock data for this outlet in the period.");
		else
		{
			var openingValue = D(stock.Sum(r => r.OpeningValue));
			var inValue = D(stock.Sum(r => r.TotalInValue));
			var outValue = D(stock.Sum(r => r.TotalOutValue));
			var closingValue = D(stock.Sum(r => r.ClosingValueByRate));

			text.AppendLine($"Stock summary: {stock.Count} items, {stock.Count(r => D(r.ClosingStock) > 0)} with closing stock, {stock.Count(r => D(r.ClosingStock) < 0)} with negative closing stock, {stock.Count(r => D(r.InStock) == 0 && D(r.OutStock) == 0)} with no movement in the period");
			text.AppendLine($"Stock value at current rate: opening={N(openingValue)}, stock in={N(inValue)}, stock out={N(outValue)}, closing={N(closingValue)}");

			var stockTypes = new (string Name, Func<ProductStockSummaryModel, decimal> Qty, Func<ProductStockSummaryModel, decimal> Value)[]
			{
				("purchase", r => D(r.PurchaseStock), r => D(r.PurchaseValue)),
				("purchase_return", r => D(r.PurchaseReturnStock), r => D(r.PurchaseReturnValue)),
				("kitchen_issue", r => D(r.KitchenIssueStock), r => D(r.KitchenIssueValue)),
				("kitchen_production", r => D(r.KitchenProductionStock), r => D(r.KitchenProductionValue)),
				("kitchen_production_return", r => D(r.KitchenProductionReturnStock), r => D(r.KitchenProductionReturnValue)),
				("sale", r => D(r.SaleStock), r => D(r.SaleValue)),
				("sale_return", r => D(r.SaleReturnStock), r => D(r.SaleReturnValue)),
				("transfer", r => D(r.StockTransferStock), r => D(r.StockTransferValue)),
				("bill", r => D(r.BillStock), r => D(r.BillValue)),
				("adjustment", r => D(r.AdjustmentStock), r => D(r.AdjustmentValue))
			};

			text.AppendLine("Stock movement by type (signed value; positive adds stock, negative removes it): " + Join(stockTypes
				.Select(m => (m.Name, Items: stock.Count(r => m.Qty(r) != 0), Value: stock.Sum(m.Value)))
				.Where(m => m.Items > 0)
				.Select(m => $"{m.Name}={N(m.Value)} ({m.Items} items)")));

			string StockItem(ProductStockSummaryModel r) => Clean(r.ProductName);

			text.AppendLine("Top 10 items by closing stock value: " + Join(stock.OrderByDescending(r => D(r.ClosingValueByRate)).Take(10)
				.Select(r => $"{StockItem(r)}: closing {N(r.ClosingStock, "0.##")} = {N(r.ClosingValueByRate)}")));

			var negative = stock.Where(r => D(r.ClosingStock) < 0).OrderBy(r => D(r.ClosingStock)).ToList();
			text.AppendLine($"Items with negative closing stock ({negative.Count}; 10 lowest): " + Join(negative.Take(10)
				.Select(r => $"{StockItem(r)}: {N(r.ClosingStock, "0.##")} (in {N(r.InStock, "0.##")}, out {N(r.OutStock, "0.##")}, opening {N(r.OpeningStock, "0.##")})")));

			text.AppendLine("Running out soonest (positive closing stock, consumption recorded, lowest days on hand): " + Join(stock
				.Where(r => D(r.ClosingStock) > 0 && D(r.OutStock) > 0)
				.OrderBy(r => D(r.DaysOnHand)).Take(5)
				.Select(r => $"{StockItem(r)}: {N(r.ClosingStock, "0.##")} left, {N(r.DaysOnHand, "0.#")} days on hand")));

			var notMoving = stock.Where(r => D(r.ClosingStock) > 0 && D(r.OutStock) == 0).OrderByDescending(r => D(r.ClosingValueByRate)).ToList();
			text.AppendLine($"Stock that did not go out at all in the period ({notMoving.Count} items, closing value {N(notMoving.Sum(r => r.ClosingValueByRate))}; 10 largest): " + Join(notMoving.Take(10)
				.Select(r => $"{StockItem(r)}: {N(r.ClosingStock, "0.##")} = {N(r.ClosingValueByRate)}")));

			// Stock count adjustments, from the movement rows (they carry dates and transaction numbers)
			var adjustments = movements.Where(r => IsAdjustment(r.Type)).ToList();
			if (adjustments.Count == 0)
				text.AppendLine("Stock count adjustments: none in the period.");
			else
			{
				text.AppendLine($"Stock count adjustments: {adjustments.Count} rows in {adjustments.Select(r => r.TransactionNo).Distinct().Count()} transactions, shortages (negative) value={N(adjustments.Where(r => r.Qty < 0).Sum(r => r.Value))}, " +
					$"surpluses (positive) value={N(adjustments.Where(r => r.Qty > 0).Sum(r => r.Value))}, net={N(adjustments.Sum(r => r.Value))}");
				text.AppendLine("Largest adjustments (date | transaction | item | quantity | value): " + Join(adjustments
					.OrderByDescending(r => Math.Abs(r.Value)).Take(10)
					.Select(r => $"{r.Date:dd-MMM-yy} | {Clean(r.TransactionNo)} | {Clean(r.Item)} | {N(r.Qty, "0.##")} | {N(r.Value)}")));
			}

			text.AppendLine($"Stock movements: {movements.Count} rows in {movements.Select(r => r.TransactionNo).Distinct().Count()} transactions; value added={N(movements.Where(r => r.Qty > 0).Sum(r => r.Value))}, value removed={N(movements.Where(r => r.Qty < 0).Sum(r => Math.Abs(r.Value)))}");
			text.AppendLine("Stock movements by type: " + Join(movements
				.GroupBy(r => r.Type, StringComparer.OrdinalIgnoreCase)
				.Select(g => new { Name = g.Key, Count = g.Count(), In = g.Where(r => r.Qty > 0).Sum(r => r.Value), Out = g.Where(r => r.Qty < 0).Sum(r => Math.Abs(r.Value)) })
				.OrderByDescending(x => x.In + x.Out)
				.Select(x => $"{x.Name}: {x.Count} rows, in={N(x.In)}, out={N(x.Out)}")));

			var noRate = movements.Where(r => r.Rate == 0 && !IsAdjustment(r.Type)).ToList();
			text.AppendLine($"Stock movements with no rate recorded (rate 0, not adjustments): {noRate.Count} rows");
		}

		// ---------- Tables ----------
		text.AppendLine();
		text.AppendLine($"DAY TABLE (CSV; one row per {(monthly ? "month" : "day")})");
		text.AppendLine("period,transactions,units,sale,sale_return,net_sale,cash,upi,card,credit,discount");

		foreach (var x in series)
			text.AppendLine(string.Join(',', Label(x.Key), x.Count.ToString(), N(x.Units, "0.##"), N(x.Total), N(x.Returns), N(x.Net), N(x.Cash), N(x.Upi), N(x.Card), N(x.Credit), N(x.Discount)));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, top 50 items by units)");
		text.AppendLine("item,category,units,net");

		foreach (var x in items.Take(50))
			text.AppendLine(string.Join(',', Clean(x.Name), Clean(x.Category), N(x.Units, "0.##"), N(x.Net)));

		text.AppendLine();
		text.AppendLine("STOCK TABLE (CSV, top 30 items by closing stock value)");
		text.AppendLine("item,category,opening,in,out,closing,closing_value,days_on_hand");

		foreach (var r in stock.OrderByDescending(s => D(s.ClosingValueByRate)).ThenBy(s => s.ProductName).Take(30))
			text.AppendLine(string.Join(',', Clean(r.ProductName), Clean(r.ProductCategoryName), N(r.OpeningStock, "0.##"), N(r.InStock, "0.##"), N(r.OutStock, "0.##"), N(r.ClosingStock, "0.##"), N(r.ClosingValueByRate), N(r.DaysOnHand, "0.#")));

		return text.ToString();
	}
}
