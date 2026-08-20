using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Payroll.Masters;

namespace PrimeBakes.Exports.Payroll.Masters;

public static class EmployeeExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<EmployeeModel> employeeData,
		IEnumerable<LocationModel> locations,
		IEnumerable<DepartmentModel> departments,
		IEnumerable<DesignationModel> designations,
		IEnumerable<UserModel> users,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = employeeData.Select(employee => new
		{
			employee.Id,
			employee.Name,
			employee.Code,
			Department = departments.FirstOrDefault(d => d.Id == employee.DepartmentId)?.Name ?? "N/A",
			Designation = designations.FirstOrDefault(d => d.Id == employee.DesignationId)?.Name ?? "N/A",
			Location = locations.FirstOrDefault(l => l.Id == employee.LocationId)?.Name ?? "N/A",
			LoginUser = users.FirstOrDefault(u => u.Id == employee.UserId)?.Name ?? "N/A",
			DateOfJoining = employee.DateOfJoining.ToString("dd/MM/yyyy"),
			DateOfLeaving = employee.DateOfLeaving?.ToString("dd/MM/yyyy") ?? "N/A",
			DateOfBirth = employee.DateOfBirth?.ToString("dd/MM/yyyy") ?? "N/A",
			employee.Gender,
			employee.FatherOrHusbandName,
			employee.Phone,
			employee.Email,
			employee.Address,
			employee.PAN,
			employee.Aadhaar,
			employee.PFNumber,
			employee.UANNumber,
			employee.ESINumber,
			employee.PaymentMode,
			employee.BankName,
			employee.BankAccountNumber,
			employee.IFSC,
			employee.Remarks,
			Status = employee.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(EmployeeModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(EmployeeModel.Name)] = new() { DisplayName = "Employee Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(EmployeeModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IsRequired = true },
			["Department"] = new() { DisplayName = "Department", Alignment = CellAlignment.Left },
			["Designation"] = new() { DisplayName = "Designation", Alignment = CellAlignment.Left },
			["Location"] = new() { DisplayName = "Location", Alignment = CellAlignment.Left },
			["LoginUser"] = new() { DisplayName = "Login User", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.DateOfJoining)] = new() { DisplayName = "Date Of Joining", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.DateOfLeaving)] = new() { DisplayName = "Date Of Leaving", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.DateOfBirth)] = new() { DisplayName = "Date Of Birth", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.Gender)] = new() { DisplayName = "Gender", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.FatherOrHusbandName)] = new() { DisplayName = "Father / Husband Name", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Phone)] = new() { DisplayName = "Phone", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Email)] = new() { DisplayName = "Email", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Address)] = new() { DisplayName = "Address", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PAN)] = new() { DisplayName = "PAN", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Aadhaar)] = new() { DisplayName = "Aadhaar", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PFNumber)] = new() { DisplayName = "PF Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.UANNumber)] = new() { DisplayName = "UAN Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.ESINumber)] = new() { DisplayName = "ESI Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.PaymentMode)] = new() { DisplayName = "Payment Mode", Alignment = CellAlignment.Center },
			[nameof(EmployeeModel.BankName)] = new() { DisplayName = "Bank Name", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.BankAccountNumber)] = new() { DisplayName = "Account Number", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.IFSC)] = new() { DisplayName = "IFSC", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(EmployeeModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(EmployeeModel.Id),
			nameof(EmployeeModel.Name),
			nameof(EmployeeModel.Code),
			"Department",
			"Designation",
			"Location",
			"LoginUser",
			nameof(EmployeeModel.DateOfJoining),
			nameof(EmployeeModel.DateOfLeaving),
			nameof(EmployeeModel.DateOfBirth),
			nameof(EmployeeModel.Gender),
			nameof(EmployeeModel.FatherOrHusbandName),
			nameof(EmployeeModel.Phone),
			nameof(EmployeeModel.Email),
			nameof(EmployeeModel.Address),
			nameof(EmployeeModel.PAN),
			nameof(EmployeeModel.Aadhaar),
			nameof(EmployeeModel.PFNumber),
			nameof(EmployeeModel.UANNumber),
			nameof(EmployeeModel.ESINumber),
			nameof(EmployeeModel.PaymentMode),
			nameof(EmployeeModel.BankName),
			nameof(EmployeeModel.BankAccountNumber),
			nameof(EmployeeModel.IFSC),
			nameof(EmployeeModel.Remarks),
			nameof(EmployeeModel.Status)
		];

		var fileName = $"Employee_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"EMPLOYEE MASTER",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"EMPLOYEE",
				"Employee Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}
