using System.Runtime.InteropServices;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Platforms.Windows;

/// <summary>
/// Sends raw ESC/POS byte data directly to the default Windows printer via the
/// print spooler (<c>winspool.drv</c>) — no dialog is shown.
/// Uses the classic "RawPrinterHelper" P/Invoke pattern described in the
/// Microsoft docs for sending raw data to a printer.
/// </summary>
public class WindowsDirectPrintService : IDirectPrintService
{
	public bool IsSupported => true;

	public Task PrintRawAsync(byte[] data)
	{
		if (data is null || data.Length == 0)
			return Task.CompletedTask;

		return Task.Run(() =>
		{
			string printerName = GetDefaultPrinterName();
			if (string.IsNullOrEmpty(printerName))
				throw new InvalidOperationException("No default printer is configured on this machine.");

			SendBytesToPrinter(printerName, data);
		});
	}

	// ── Get the default printer name ────────────────────────────────────────

	private static string GetDefaultPrinterName()
	{
		int size = 0;
		GetDefaultPrinter(null, ref size);

		if (size <= 0)
			return string.Empty;

		var buffer = new char[size];
		if (GetDefaultPrinter(buffer, ref size))
			return new string(buffer, 0, size - 1); // trim null terminator

		return string.Empty;
	}

	// ── Send raw bytes to a named printer ───────────────────────────────────

	private static void SendBytesToPrinter(string printerName, byte[] data)
	{
		var docInfo = new DOC_INFO_1
		{
			pDocName = "PrimeBakes Thermal Receipt",
			pDataType = "RAW"
		};

		if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
			throw new InvalidOperationException($"Cannot open printer '{printerName}'.");

		try
		{
			if (!StartDocPrinter(hPrinter, 1, ref docInfo))
				throw new InvalidOperationException("StartDocPrinter failed.");

			try
			{
				if (!StartPagePrinter(hPrinter))
					throw new InvalidOperationException("StartPagePrinter failed.");

				try
				{
					IntPtr pBytes = Marshal.AllocCoTaskMem(data.Length);
					try
					{
						Marshal.Copy(data, 0, pBytes, data.Length);
						if (!WritePrinter(hPrinter, pBytes, data.Length, out _))
							throw new InvalidOperationException("WritePrinter failed.");
					}
					finally
					{
						Marshal.FreeCoTaskMem(pBytes);
					}
				}
				finally
				{
					EndPagePrinter(hPrinter);
				}
			}
			finally
			{
				EndDocPrinter(hPrinter);
			}
		}
		finally
		{
			ClosePrinter(hPrinter);
		}
	}

	// ── P/Invoke declarations (winspool.drv) ────────────────────────────────

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DOC_INFO_1
	{
		[MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
		[MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
		[MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
	}

	[DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool GetDefaultPrinter(char[] pszBuffer, ref int pcchBuffer);

	[DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

	[DllImport("winspool.drv", SetLastError = true)]
	private static extern bool ClosePrinter(IntPtr hPrinter);

	[DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 pDocInfo);

	[DllImport("winspool.drv", SetLastError = true)]
	private static extern bool EndDocPrinter(IntPtr hPrinter);

	[DllImport("winspool.drv", SetLastError = true)]
	private static extern bool StartPagePrinter(IntPtr hPrinter);

	[DllImport("winspool.drv", SetLastError = true)]
	private static extern bool EndPagePrinter(IntPtr hPrinter);

	[DllImport("winspool.drv", SetLastError = true)]
	private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
