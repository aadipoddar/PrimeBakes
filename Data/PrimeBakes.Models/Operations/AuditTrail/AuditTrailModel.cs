namespace PrimeBakes.Models.Operations.AuditTrail;

public class AuditTrailModel
{
	public int Id { get; set; }
	public string Action { get; set; }
	public string TableName { get; set; }
	public string RecordNo { get; set; }
	public string? RecordValue { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime TransactionDateTime { get; set; } = DateTime.Now;
	public string? CreatedFormFactor { get; set; }
	public string? CreatedPlatform { get; set; }
	public decimal? CreatedLatitude { get; set; }
	public decimal? CreatedLongitude { get; set; }
}

public enum AuditTrailActionTypes
{
	Insert,
	Update,
	Delete,
	Recover,
	Report
}
