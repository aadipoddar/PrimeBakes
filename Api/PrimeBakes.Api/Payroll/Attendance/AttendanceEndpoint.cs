using PrimeBakes.Data.Payroll.Attendance;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Payroll.Attendance;

public class AttendanceEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(AttendanceEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(AttendanceData.LoadAttendanceOverviewByEmployeeMonthYear),
			(int? EmployeeId, int? AttendanceMonth, int? AttendanceYear) => AttendanceData.LoadAttendanceOverviewByEmployeeMonthYear(EmployeeId, AttendanceMonth, AttendanceYear));

		group.MapPost(nameof(AttendanceData.DeleteTransaction), AttendanceData.DeleteTransaction);
		group.MapPost(nameof(AttendanceData.RecoverTransaction), AttendanceData.RecoverTransaction);
		group.MapPost(nameof(AttendanceData.SaveTransaction), AttendanceData.SaveTransaction);
	}
}
