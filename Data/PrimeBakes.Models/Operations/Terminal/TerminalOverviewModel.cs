namespace PrimeBakes.Models.Operations.Terminal;

public class TerminalOverviewModel
{
	public int Id { get; set; }
	public int TerminalNo { get; set; }
	public string MachineName { get; set; }
	public string MachineId { get; set; }
	public int UserId { get; set; }
	public string UserName { get; set; }
	public DateTime? LastSyncedAt { get; set; }
}
