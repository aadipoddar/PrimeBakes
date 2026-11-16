using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Analysis;

namespace PrimeBakes.Shared.Components.Ai;

public partial class ReportAi
{
	private const string FallbackQuestion = "Give exactly five one-line bullets summarizing this report, then one thing that needs attention, or say that nothing does.";

	[Parameter] public string Title { get; set; } = "AI Insights";

	/// <summary>Page specific prompt: what the report is and the business rules for reading it (see ReportAiPrompts).</summary>
	[Parameter, EditorRequired] public string Instructions { get; set; }

	/// <summary>Builds the page's current period, filters and data as text. Called only when the user asks.</summary>
	[Parameter, EditorRequired] public Func<string> GetContext { get; set; }

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
			var prompt = ReportAiPrompts.Build(Instructions, GetContext?.Invoke(), question);
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
