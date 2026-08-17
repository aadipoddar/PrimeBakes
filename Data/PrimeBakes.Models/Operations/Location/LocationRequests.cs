namespace PrimeBakes.Models.Operations.Location;

public sealed record LocationSaveRequest(
	LocationModel Location,
	LocationModel CopyLocation);
