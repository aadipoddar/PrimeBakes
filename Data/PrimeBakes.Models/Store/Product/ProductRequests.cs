using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Models.Store.Product;

public sealed record ProductSaveRequest(
	ProductModel Product,
	List<LocationModel> Locations,
	DateOnly EffectiveDate);
