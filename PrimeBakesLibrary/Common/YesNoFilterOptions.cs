namespace PrimeBakesLibrary.Common;

public sealed record YesNoFilterOption(int Id, string Name);

public static class YesNoFilterOptions
{
	public const int All = 0;
	public const int Yes = 1;
	public const int No = 2;

	public static readonly List<YesNoFilterOption> YesNo =
	[
		new(Yes, "Yes"),
		new(No, "No"),
	];
}
