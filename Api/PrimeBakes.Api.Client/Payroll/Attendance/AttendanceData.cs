using PrimeBakes.Models.Common;
using PrimeBakes.Models.Payroll.Attendance;

namespace PrimeBakes.Data.Payroll.Attendance;

public static class AttendanceData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(AttendanceData));

	public static async Task<List<AttendanceOverviewModel>> LoadAttendanceOverviewByEmployeeMonthYear(int? EmployeeId = null, int? AttendanceMonth = null, int? AttendanceYear = null) =>
		await ApiClient.Get<List<AttendanceOverviewModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadAttendanceOverviewByEmployeeMonthYear)), new { EmployeeId, AttendanceMonth, AttendanceYear });

	public static async Task DeleteTransaction(AttendanceModel attendance, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), attendance, new { userId, platform });

	public static async Task RecoverTransaction(AttendanceModel attendance, int userId, string platform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), attendance, new { userId, platform });

	public static async Task<int> SaveTransaction(AttendanceModel attendance, int userId, string platform) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)), attendance, new { userId, platform });
}
