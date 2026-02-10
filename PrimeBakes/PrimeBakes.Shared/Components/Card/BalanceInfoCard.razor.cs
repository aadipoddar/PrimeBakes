using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Card;

public partial class BalanceInfoCard
{
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public string Label { get; set; } = string.Empty;
    [Parameter] public string Value { get; set; } = string.Empty;
    [Parameter] public string? Breakdown { get; set; }
    [Parameter] public CardVariant Variant { get; set; } = CardVariant.Default;

    private string GetVariantClass() => Variant switch
    {
        CardVariant.Opening => "opening-card",
        CardVariant.Debit => "debit-card",
        CardVariant.Credit => "credit-card",
        CardVariant.Closing => "closing-card",
        CardVariant.Assets => "assets-card",
        CardVariant.Liabilities => "liabilities-card",
        CardVariant.Positive => "positive-card",
        CardVariant.Negative => "negative-card",
        CardVariant.Income => "income-card",
        CardVariant.Expense => "expense-card",
        CardVariant.Profit => "profit-card",
        CardVariant.Loss => "loss-card",
        _ => ""
    };

    private string GetAmountClass() => Variant switch
    {
        CardVariant.Debit => "debit-amount",
        CardVariant.Credit => "credit-amount",
        CardVariant.Assets => "assets-amount",
        CardVariant.Liabilities => "liabilities-amount",
        CardVariant.Positive => "positive-amount",
        CardVariant.Negative => "negative-amount",
        CardVariant.Income => "credit-amount",
        CardVariant.Expense => "debit-amount",
        CardVariant.Profit => "profit-amount",
        CardVariant.Loss => "loss-amount",
        _ => ""
    };

    public enum CardVariant
    {
        Default,
        Opening,
        Debit,
        Credit,
        Closing,
        Assets,
        Liabilities,
        Positive,
        Negative,
        Income,
        Expense,
        Profit,
        Loss
    }
}