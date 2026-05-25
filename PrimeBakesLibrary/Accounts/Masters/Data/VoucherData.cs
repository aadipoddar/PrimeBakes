using PrimeBakesLibrary.Accounts.Masters.Models;

namespace PrimeBakesLibrary.Accounts.Masters.Data;

public static class VoucherData
{
    public static async Task<int> InsertVoucher(VoucherModel voucher) =>
        (await SqlDataAccess.LoadData<int, dynamic>(AccountNames.InsertVoucher, voucher)).FirstOrDefault();
}