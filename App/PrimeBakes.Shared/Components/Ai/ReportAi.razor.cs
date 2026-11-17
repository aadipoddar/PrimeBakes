using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Analysis;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Components.Ai;

public partial class ReportAi
{
	// Shared rules for every report page. Page specific rules come from the page's own Instructions.
	private const string Base = """
		You are the business analyst for Prime Bakes, a bakery company. All money is in Indian rupees (₹).

		RULES
		- Use only the data below. Never invent numbers, splits or causes. If the data cannot answer the question, say what is missing.
		- Where the data has a PRECOMPUTED TOTALS section, quote those figures. Do not add up rows yourself.
		- Copy location, product and party names exactly as written.
		- Round amounts to whole rupees and write them with Indian digit grouping, for example ₹14,60,750. Write percentages with the % sign.
		- The latest day or month in a period can be incomplete. Never call a partial period a drop or a fall because it is smaller than a full one.

		STYLE
		- Plain text only: no markdown, no asterisks, no headings. Use "-" for bullets.
		- Short and direct. If the question is specific, answer it first in one or two sentences, then add at most 3 supporting bullets. If the question asks for a fixed list, give exactly that list, with no preamble.
		""";

	private static string BuildPrompt(string pageInstructions, string data, string question) => $"""
		{Base}

		{pageInstructions}

		REPORT DATA
		{data}

		QUESTION
		{question}
		""";

	private const string FallbackQuestion = "Give exactly five one-line bullets summarizing this report, then one thing that needs attention, or say that nothing does.";

	[Parameter] public string Title { get; set; } = "AI Insights";

	/// <summary>Page specific prompt: what the report is and the business rules for reading it (declared in the page's own .Ai.cs).</summary>
	[Parameter, EditorRequired] public string Instructions { get; set; }

	/// <summary>Builds the page's current period, filters and data as text. Called only when the user asks.</summary>
	[Parameter, EditorRequired] public Func<string> GetContext { get; set; }
	[Parameter, EditorRequired] public string ReportName { get; set; }
	[Parameter, EditorRequired] public int UserId { get; set; }

	/// <summary>Used when the question box is empty, so briefings are comparable run to run.</summary>
	[Parameter] public string DefaultQuestion { get; set; }

	[Parameter] public IReadOnlyList<string> Suggestions { get; set; }

	private string _question = string.Empty;
	private string _answer = string.Empty;
	private bool _isBusy;

	private Task Generate() => Run(_question);

	private Task Suggest(string suggestion)
	{
		_question = suggestion;
		return Run(suggestion);
	}

	private async Task Run(string input)
	{
		if (_isBusy)
			return;

		var question = !string.IsNullOrWhiteSpace(input) ? input.Trim()
			: !string.IsNullOrWhiteSpace(DefaultQuestion) ? DefaultQuestion
			: FallbackQuestion;

		_isBusy = true;
		_answer = string.Empty;   // empty answer lets the "thinking" placeholder show

		try
		{
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.AI.ToString(),
				TableName = ReportName,
				RecordNo = "",
				RecordValue = question,
				CreatedBy = UserId
			});

			var prompt = BuildPrompt(Instructions, GetContext?.Invoke(), question);
			_answer = await GeminiData.Ask(prompt);
		}
		catch (Exception ex)
		{
			_answer = $"AI request failed: {ex.Message}";
		}
		finally
		{
			_isBusy = false;
		}
	}
}
