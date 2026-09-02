namespace PrimeBakes.Models.Operations.Maintenance;

public class SyncVersionModel
{
	public int Id { get; set; }
	public string TableName { get; set; }
	public long Version { get; set; }
	public DateTime LastSyncedAt { get; set; }
}
