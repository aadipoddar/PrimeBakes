using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Shared.Components.Grid;

public partial class AuditTrailGrid
{
	[Parameter, EditorRequired] public string RecordNo { get; set; }

	private bool _isLoading;

	private List<AuditTrailModel> _auditTrails = [];

	protected override async Task OnParametersSetAsync()
	{
		_auditTrails = [];

		var recordNo = RecordNo?.Trim();
		if (string.IsNullOrWhiteSpace(recordNo))
			return;

		_isLoading = true;
		try
		{
			var records = await AuditTrailData.LoadAuditTrailByRecordNo(recordNo);
			_auditTrails = [.. records.OrderByDescending(record => record.TransactionDateTime)];
		}
		finally
		{
			_isLoading = false;
		}
	}
}
