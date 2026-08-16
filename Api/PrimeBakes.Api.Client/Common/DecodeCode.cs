using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Common;

public static class DecodeCode
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(DecodeCode));

	public static async Task<DecodeTransactionNoModel> DecodeTransactionNo(string transactionNo, bool pdf = true, bool excel = true, CodeType? codeType = null)
	{
		var result = await ApiClient.Get<DecodeTransactionNoResult>(
			Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DecodeTransactionNo)),
			new { transactionNo, pdf, excel, codeType });

		if (result is null)
			return null;

		return new DecodeTransactionNoModel
		{
			CodeType = result.CodeType,
			PageRouteName = result.PageRouteName,
			PDFStream = ToStream(result.Pdf),
			ExcelStream = ToStream(result.Excel),
		};
	}

	private static (MemoryStream stream, string fileName) ToStream(FileResult file) =>
		file?.Bytes is null ? default : (new MemoryStream(file.Bytes), file.FileName);
}
