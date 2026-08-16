namespace PrimeBakes.Models.Restaurant.Bill;

public sealed record KOTThermalRequest(
	int BillId,
	int KotCategoryId,
	List<BillItemCartModel> KotItems);
