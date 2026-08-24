namespace PrimeBakes.Models.Store.Summary;

public class OrderItemMonthlySummaryModel
{
	private readonly decimal[] _months = new decimal[12];

	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }

	public decimal this[int monthIndex]
	{
		get => _months[monthIndex];
		set => _months[monthIndex] = value;
	}

	public decimal Month1 { get => _months[0]; set => _months[0] = value; }
	public decimal Month2 { get => _months[1]; set => _months[1] = value; }
	public decimal Month3 { get => _months[2]; set => _months[2] = value; }
	public decimal Month4 { get => _months[3]; set => _months[3] = value; }
	public decimal Month5 { get => _months[4]; set => _months[4] = value; }
	public decimal Month6 { get => _months[5]; set => _months[5] = value; }
	public decimal Month7 { get => _months[6]; set => _months[6] = value; }
	public decimal Month8 { get => _months[7]; set => _months[7] = value; }
	public decimal Month9 { get => _months[8]; set => _months[8] = value; }
	public decimal Month10 { get => _months[9]; set => _months[9] = value; }
	public decimal Month11 { get => _months[10]; set => _months[10] = value; }
	public decimal Month12 { get => _months[11]; set => _months[11] = value; }

	public decimal FulfilledQuantity { get; set; }
	public decimal PendingQuantity { get; set; }

	public int OrderCount { get; set; }
	public int FulfilledOrderCount { get; set; }
	public int LocationCount { get; set; }
	public int Rank { get; set; }
	public decimal ContributionPercent { get; set; }
	public string PeakMonthName { get; set; }
	public string LowestMonthName { get; set; }
	public DateTime? FirstOrderDateTime { get; set; }
	public DateTime? LastOrderDateTime { get; set; }
	public int MonthsSinceLastOrder { get; set; }

	public decimal Total => _months.Sum();
	public int ActiveMonths => _months.Count(month => month != 0);

	public decimal AveragePerMonth => Math.Round(Total / 12, 2);
	public decimal AveragePerActiveMonth => ActiveMonths == 0 ? 0 : Math.Round(Total / ActiveMonths, 2);
	public decimal AveragePerOrder => OrderCount == 0 ? 0 : Math.Round(Total / OrderCount, 2);

	public decimal FulfilmentPercent => Total == 0 ? 0 : Math.Round(FulfilledQuantity / Total * 100, 2);
	public decimal PendingPercent => Total == 0 ? 0 : Math.Round(PendingQuantity / Total * 100, 2);

	public decimal PeakMonthValue => _months.Max();
	public decimal LowestMonthValue => ActiveMonths == 0 ? 0 : _months.Where(month => month != 0).Min();

	public decimal Quarter1 => _months.Take(3).Sum();
	public decimal Quarter2 => _months.Skip(3).Take(3).Sum();
	public decimal Quarter3 => _months.Skip(6).Take(3).Sum();
	public decimal Quarter4 => _months.Skip(9).Take(3).Sum();

	public decimal FirstHalf => _months.Take(6).Sum();
	public decimal SecondHalf => _months.Skip(6).Sum();
	public decimal GrowthPercent => FirstHalf == 0 ? 0 : Math.Round((SecondHalf - FirstHalf) / FirstHalf * 100, 2);
	public decimal RecentTrendPercent => Quarter3 == 0 ? 0 : Math.Round((Quarter4 - Quarter3) / Quarter3 * 100, 2);

	public decimal ConsistencyPercent
	{
		get
		{
			var mean = (double)Total / 12;
			if (mean == 0)
				return 0;

			var deviation = Math.Sqrt(_months.Sum(month => Math.Pow((double)month - mean, 2)) / 12);
			return Math.Round((decimal)Math.Clamp(100 - (deviation / mean * 100), 0, 100), 2);
		}
	}

	public static List<string> BuildMonthHeaders(DateOnly financialYearStart) =>
		[.. Enumerable.Range(0, 12).Select(offset => financialYearStart.AddMonths(offset).ToString("MMM yy"))];
}
