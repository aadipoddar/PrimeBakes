using Microsoft.SqlServer.Dac;

using PrimeBakes.Data.Operations.Maintenance;
using PrimeBakes.Shared.Services.Host;

using System.Diagnostics;

namespace PrimeBakes.Platforms.Windows;

public static class LocalDbManager
{
	private const string _instance = "AadiSoft";
	private const string _database = "PrimeBakesClient";
	private const string _schemaVersionKey = "LocalSchemaVersion";

	private static readonly SemaphoreSlim _gate = new(1, 1);

	public static async Task SyncDataBackground()
	{
		if (!await _gate.WaitAsync(0))
			return;

		try
		{
			var version = typeof(ILocalDbService).Assembly.GetName().Version?.ToString();

			if (Preferences.Get(_schemaVersionKey, string.Empty) != version)
			{
				await Task.Run(SetupDatabase);
				Preferences.Set(_schemaVersionKey, version);
			}

			await SyncData.SyncToLocalClient();
		}
		catch { }
		finally
		{
			_gate.Release();
		}
	}

	public static async Task InstallSqlServer() =>
		await RunScript("primebakes_localdb_install.ps1", _installScript);

	public static async Task UninstallSqlServer() =>
		await RunScript("primebakes_localdb_uninstall.ps1", _uninstallScript);

	public static void SetupDatabase()
	{
		var dacpacPath = Path.Combine(AppContext.BaseDirectory, "PrimeBakes.Database.dacpac");

		if (!File.Exists(dacpacPath))
			throw new FileNotFoundException("PrimeBakes.Database.dacpac was not found next to the app.");

		using var package = DacPackage.Load(dacpacPath);

		DacServices services = new($@"Server=.\{_instance};Integrated Security=True;TrustServerCertificate=True");
		services.Deploy(package, _database, true, new()
		{
			AllowIncompatiblePlatform = true,
			BlockOnPossibleDataLoss = false
		});
	}

	private static async Task RunScript(string fileName, string script)
	{
		var scriptPath = Path.Combine(Path.GetTempPath(), fileName);
		File.WriteAllText(scriptPath, script);

		var startInfo = new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
			UseShellExecute = true,
			Verb = "runas",
			CreateNoWindow = false
		};

		using var process = Process.Start(startInfo);
		if (process is not null)
			await process.WaitForExitAsync();
	}

	private const string _installScript = """
		$ErrorActionPreference = 'Stop'
		$Instance = 'AadiSoft'; $Work = 'C:\Temp\Sql'

		try {
			$pendingReboot = (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending') -or
				(Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired') -or
				($null -ne (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager' -Name PendingFileRenameOperations -ErrorAction SilentlyContinue))

			if ($pendingReboot) {
				Write-Host 'This computer must be restarted before SQL Server can be installed.'
				Write-Host 'Please restart, then run Install SQL Server again.'
				return
			}

			$existing = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server' -Name InstalledInstances -ErrorAction SilentlyContinue).InstalledInstances

			if ($existing -contains $Instance) {
				Write-Host "SQL Server instance $Instance is already installed."
				return
			}

			New-Item -ItemType Directory -Force -Path $Work | Out-Null

			Write-Host 'Step 1 of 4 - Downloading installer...'
			Invoke-WebRequest 'https://go.microsoft.com/fwlink/?linkid=2216019' -OutFile "$Work\SSEI.exe"

			Write-Host 'Step 2 of 4 - Downloading SQL Server Express (~1 GB, several minutes)...'
			Start-Process "$Work\SSEI.exe" -Wait -ArgumentList "/ACTION=Download /MEDIAPATH=$Work /MEDIATYPE=Core /QUIET"

			Write-Host 'Step 3 of 4 - Extracting...'
			$media = Get-ChildItem $Work -Filter 'SQLEXPR*.exe' | Select-Object -First 1
			Start-Process $media.FullName -Wait -ArgumentList ('/q /x:"' + $Work + '\setup"')

			Write-Host 'Step 4 of 4 - Installing...'
			$setup = Start-Process "$Work\setup\setup.exe" -Wait -PassThru -ArgumentList @(
				'/QS','/ACTION=Install','/FEATURES=SQL',"/INSTANCENAME=$Instance",
				'/IACCEPTSQLSERVERLICENSETERMS','/ADDCURRENTUSERASSQLADMIN=True','/TCPENABLED=0')

			Remove-Item -LiteralPath $Work -Recurse -Force -ErrorAction SilentlyContinue

			if ($setup.ExitCode -eq 3010 -or $setup.ExitCode -eq -2067919934) {
				Write-Host ''
				Write-Host 'SQL Server needs this computer to restart before it can be installed.'
				Write-Host 'Please restart, then run Install SQL Server again.'
				return
			}

			if ($setup.ExitCode -ne 0) {
				Write-Host ''
				Write-Host "SQL Server setup failed with exit code $($setup.ExitCode)."
				Write-Host 'Open Setup Bootstrap\Log\Summary.txt under C:\Program Files\Microsoft SQL Server for details.'
				return
			}

			Write-Host ''
			Write-Host 'SQL Server installed.'
		}
		catch {
			Write-Host ''
			Write-Host "Install failed: $($_.Exception.Message)"
		}
		finally {
			Write-Host ''
			Pause
		}
		""";

	private const string _uninstallScript = """
		$ErrorActionPreference = 'Continue'
		$Instance = 'AadiSoft'

		$existing = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server' -Name InstalledInstances -ErrorAction SilentlyContinue).InstalledInstances

		if ($existing -contains $Instance) {
			Write-Host 'Uninstalling SQL Server instance (several minutes)...'
			$setup = Get-ChildItem 'C:\Program Files\Microsoft SQL Server\*\Setup Bootstrap\*\setup.exe' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime | Select-Object -Last 1
			if ($setup) {
				Start-Process $setup.FullName -Wait -ArgumentList @('/Q','/ACTION=Uninstall','/FEATURES=SQL',"/INSTANCENAME=$Instance")
			}
			else {
				Write-Host 'SQL Server setup was not found. Remove the instance from Apps and Features manually.'
			}

			$remaining = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server' -Name InstalledInstances -ErrorAction SilentlyContinue).InstalledInstances

			if ($remaining -notcontains $Instance) {
				Write-Host 'Removing database files...'
				Get-ChildItem 'C:\Program Files\Microsoft SQL Server' -Directory -Filter "MSSQL*.$Instance" -ErrorAction SilentlyContinue |
					Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
			}

			Remove-Item -LiteralPath 'C:\Temp\Sql' -Recurse -Force -ErrorAction SilentlyContinue
		}
		""";
}
