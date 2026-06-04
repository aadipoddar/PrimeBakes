using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Data.Inventory;

public static class RawMaterialData
{
    public static async Task<int> InsertRawMaterial(RawMaterialModel rawMaterial, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRawMaterial, rawMaterial, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<int> InsertRawMaterialCategory(RawMaterialCategoryModel rawMaterialCategory) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRawMaterialCategory, rawMaterialCategory)).FirstOrDefault();
}