namespace PrimeBakes.Models.Operations.Terminal;

public class TerminalModel
{
	public int Id { get; set; }
	public int TerminalNo { get; set; }
	public string MachineId { get; set; }
	public string? MachineName { get; set; }
	public int UserId { get; set; }
	public DateTime? LastSyncedAt { get; set; }
	public bool Status { get; set; }
}