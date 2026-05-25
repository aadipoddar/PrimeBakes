using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Operations.AuditTrail.Models;
using PrimeBakesLibrary.Operations.User.Models;

using System.Reflection;
using System.Text;

namespace PrimeBakesLibrary.Operations.AuditTrail.Data;

public static class AuditTrailData
{
	private static async Task<int> InsertAuditTrail(AuditTrailModel auditTrail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertAuditTrail, auditTrail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Audit Trail.");

	public static async Task SaveAuditTrail(AuditTrailModel auditTrail, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, auditTrail.CreatedBy, sqlDataAccessTransaction);
		auditTrail.CreatedByName = user.Name;
		await InsertAuditTrail(auditTrail, sqlDataAccessTransaction);
	}

	private static readonly HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase)
	{
		"Id", "MasterId", "Status",
		"CreatedBy", "CreatedByName", "CreatedAt", "CreatedFromPlatform",
		"LastModifiedBy", "LastModifiedByUserName", "LastModifiedAt", "LastModifiedFromPlatform"
	};

	public static string GetDifference<T>(T previous, T current)
	{
		var lines = new List<string>();

		foreach (var property in GetAuditableProperties(typeof(T)))
		{
			var oldValue = previous is null ? null : property.GetValue(previous);
			var newValue = current is null ? null : property.GetValue(current);

			if (Equals(oldValue, newValue))
				continue;

			lines.Add($"{FormatName(property.Name)}: {FormatValue(oldValue)} -> {FormatValue(newValue)}");
		}

		return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
	}

	public static string GetDifference<T>(List<T> previous, List<T> current, Type masterType = null)
	{
		previous ??= [];
		current ??= [];

		var properties = GetAuditableProperties(typeof(T), masterType);
		var matched = new HashSet<int>();
		var removed = new List<T>();

		foreach (var prev in previous)
		{
			var match = -1;
			for (var i = 0; i < current.Count; i++)
				if (!matched.Contains(i) && properties.All(p => Equals(p.GetValue(prev), p.GetValue(current[i]))))
				{
					match = i;
					break;
				}

			if (match < 0) removed.Add(prev);
			else matched.Add(match);
		}

		var added = current.Where((_, i) => !matched.Contains(i)).ToList();
		var sections = new List<string>();

		if (removed.Count > 0)
			sections.Add("Removed:" + Environment.NewLine + string.Join(Environment.NewLine, removed.Select(item => $"  - {Summarize(item, properties)}")));

		if (added.Count > 0)
			sections.Add("Added:" + Environment.NewLine + string.Join(Environment.NewLine, added.Select(item => $"  - {Summarize(item, properties)}")));

		return sections.Count == 0 ? null : string.Join(Environment.NewLine, sections);
	}

	public static string CombineDifferences(params (string Label, string Diff)[] sections)
	{
		var blocks = sections
			.Where(s => !string.IsNullOrWhiteSpace(s.Diff))
			.Select(s => string.IsNullOrWhiteSpace(s.Label) ? s.Diff : $"{s.Label}:{Environment.NewLine}{s.Diff}")
			.ToList();

		return blocks.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, blocks);
	}

	private static List<PropertyInfo> GetAuditableProperties(Type type, Type excludeFromType = null)
	{
		var excluded = excludeFromType?
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Select(p => p.Name)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => IsAuditable(p) && (excluded is null || !excluded.Contains(p.Name)))];
	}

	private static bool IsAuditable(PropertyInfo property)
	{
		if (_ignoredProperties.Contains(property.Name)) return false;

		var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
		return !(type == typeof(int) && property.Name.EndsWith("Id", StringComparison.Ordinal));
	}

	private static string Summarize<T>(T item, List<PropertyInfo> properties)
	{
		var parts = new List<string>();
		foreach (var property in properties)
		{
			var value = property.GetValue(item);
			if (value is null) continue;
			if (value is string s && string.IsNullOrWhiteSpace(s)) continue;

			parts.Add($"{FormatName(property.Name)}: {FormatValue(value)}");
		}
		return string.Join(" | ", parts);
	}

	private static string FormatName(string name)
	{
		var sb = new StringBuilder(name.Length + 4);
		for (var i = 0; i < name.Length; i++)
		{
			if (i > 0 && char.IsUpper(name[i]) &&
				(!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && !char.IsUpper(name[i + 1]))))
				sb.Append(' ');
			sb.Append(name[i]);
		}
		return sb.ToString();
	}

	private static string FormatValue(object value) => value switch
	{
		null => "(empty)",
		string s when string.IsNullOrWhiteSpace(s) => "(empty)",
		string s => s,
		bool b => b ? "Yes" : "No",
		decimal d => d.ToString("N2"),
		double d => d.ToString("N2"),
		float f => f.ToString("N2"),
		DateTime dt => dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("dd-MMM-yyyy") : dt.ToString("dd-MMM-yyyy HH:mm"),
		DateOnly d => d.ToString("dd-MMM-yyyy"),
		TimeOnly t => t.ToString("HH:mm"),
		_ => value.ToString()
	};
}
