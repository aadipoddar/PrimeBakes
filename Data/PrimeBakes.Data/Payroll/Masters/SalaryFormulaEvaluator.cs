using System.Text.RegularExpressions;

using NCalc;

using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Data.Payroll.Masters;

public static class SalaryFormulaEvaluator
{
	public const string FullMonthPrefix = "FULL_";

	public static readonly string[] ReservedVariables = ["OTHOURS", "PAIDDAYS", "DAYSINMONTH"];

	public static bool FormulaReferences(string formula, string code) =>
		Regex.IsMatch(formula, $@"\b{Regex.Escape(code)}\b", RegexOptions.IgnoreCase);

	public static decimal Evaluate(string formula, Dictionary<string, decimal> variables)
	{
		var expression = new Expression(formula, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);

		foreach (var variable in variables)
			expression.Parameters[variable.Key] = variable.Value;

		var result = expression.Evaluate()
			?? throw new InvalidOperationException("The formula did not produce a value.");

		return Convert.ToDecimal(result);
	}

	public static void ValidateFormula(string formula, IEnumerable<string> availableCodes)
	{
		var expression = new Expression(formula, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);

		if (expression.HasErrors())
			throw new InvalidOperationException("The formula is not valid. Please check the brackets and operators.");

		var variables = new Dictionary<string, decimal>();
		foreach (var code in availableCodes)
		{
			variables[code] = 1;
			variables[FullMonthPrefix + code] = 1;
		}

		foreach (var reserved in ReservedVariables)
			variables[reserved] = 1;

		try
		{
			Evaluate(formula, variables);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"The formula could not be calculated: {ex.Message}");
		}
	}

	public static Dictionary<int, decimal> EvaluateAll(
		List<SalaryComponentModel> components,
		Dictionary<int, decimal> inputAmounts,
		decimal paidDays,
		decimal daysInMonth,
		Dictionary<string, decimal> extraVariables = null)
	{
		var results = new Dictionary<int, decimal>();
		var variables = new Dictionary<string, decimal>();
		var fullMonthVariables = new Dictionary<string, decimal>();

		foreach (var reserved in ReservedVariables)
		{
			variables[reserved] = 0;
			fullMonthVariables[reserved] = 0;
		}

		variables["PAIDDAYS"] = paidDays;
		variables["DAYSINMONTH"] = daysInMonth;
		fullMonthVariables["PAIDDAYS"] = daysInMonth;
		fullMonthVariables["DAYSINMONTH"] = daysInMonth;

		if (extraVariables is not null)
			foreach (var extraVariable in extraVariables)
			{
				variables[extraVariable.Key] = extraVariable.Value;
				fullMonthVariables[extraVariable.Key] = extraVariable.Value;
			}

		foreach (var component in components.OrderBy(c => c.Sequence))
		{
			var hasFormula = !string.IsNullOrWhiteSpace(component.Formula);

			var amount = hasFormula
				? Evaluate(component.Formula, variables)
				: inputAmounts.GetValueOrDefault(component.Id);

			var fullMonthAmount = hasFormula
				? Evaluate(component.Formula, fullMonthVariables)
				: inputAmounts.GetValueOrDefault(component.Id);

			if (component.Prorate && daysInMonth > 0)
				amount = amount * paidDays / daysInMonth;

			if (component.Rounding)
			{
				amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
				fullMonthAmount = Math.Round(fullMonthAmount, 0, MidpointRounding.AwayFromZero);
			}

			results[component.Id] = amount;

			variables[component.Code] = amount;
			variables[FullMonthPrefix + component.Code] = fullMonthAmount;

			fullMonthVariables[component.Code] = fullMonthAmount;
			fullMonthVariables[FullMonthPrefix + component.Code] = fullMonthAmount;
		}

		return results;
	}
}
