using PrimeBakesLibrary.Store.Masters.Models;

namespace PrimeBakesLibrary.Store.Masters.Data;

public static class PaymentModeData
{
    public static List<PaymentModeModel> GetPaymentModes() =>
            [
                new() { Id = 1, Name = "Cash" },
                new() { Id = 2, Name = "Card" },
                new() { Id = 3, Name = "UPI" },
                new() { Id = 4, Name = "Credit" }
            ];
}
