using System.Diagnostics;

namespace PrimeBakes.Platforms.Windows;

public static class LocalDbManager
{
	public static void RunSetup()
	{
		var scriptPath = Path.Combine(Path.GetTempPath(), "primebakes_localdb.ps1");
		File.WriteAllText(scriptPath, _setupScript);

		var startInfo = new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
			UseShellExecute = true,
			Verb = "runas",
			CreateNoWindow = false
		};

		Process.Start(startInfo);
	}

	public static async Task RunUninstall()
	{
		var scriptPath = Path.Combine(Path.GetTempPath(), "primebakes_localdb_uninstall.ps1");
		File.WriteAllText(scriptPath, _uninstallScript);

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

	private const string _setupScript = """
		$ErrorActionPreference = 'Stop'
		$Instance = 'AadiSoft'; $Database = 'PrimeBakesClient'; $Work = 'C:\Temp\Sql'
		New-Item -ItemType Directory -Force -Path $Work | Out-Null

		$existing = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server' -Name InstalledInstances -ErrorAction SilentlyContinue).InstalledInstances

		if ($existing -contains $Instance) {
			Write-Host "SQL Server instance $Instance is already installed."
		}
		else {
			Write-Host 'Step 1 of 4 - Downloading installer...'
			Invoke-WebRequest 'https://go.microsoft.com/fwlink/?linkid=2216019' -OutFile "$Work\SSEI.exe"

			Write-Host 'Step 2 of 4 - Downloading SQL Server Express (~1 GB, several minutes)...'
			Start-Process "$Work\SSEI.exe" -Wait -ArgumentList "/ACTION=Download /MEDIAPATH=$Work /MEDIATYPE=Core /QUIET"

			Write-Host 'Step 3 of 4 - Extracting...'
			$media = Get-ChildItem $Work -Filter 'SQLEXPR*.exe' | Select-Object -First 1
			Start-Process $media.FullName -Wait -ArgumentList ('/q /x:"' + $Work + '\setup"')

			Write-Host 'Step 4 of 4 - Installing...'
			Start-Process "$Work\setup\setup.exe" -Wait -ArgumentList @(
				'/QS','/ACTION=Install','/FEATURES=SQL',"/INSTANCENAME=$Instance",
				'/IACCEPTSQLSERVERLICENSETERMS','/ADDCURRENTUSERASSQLADMIN=True','/TCPENABLED=0')
		}

		Write-Host 'Creating database...'
		$connection = New-Object System.Data.SqlClient.SqlConnection("Server=.\$Instance;Integrated Security=True;TrustServerCertificate=True")
		$connection.Open()
		$command = $connection.CreateCommand()
		$command.CommandText = "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];"
		$command.ExecuteNonQuery() | Out-Null
		$connection.Close()

		Write-Host ''
		Write-Host 'Done. Please reopen Prime Bakes.'
		Pause
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

			Remove-Item -LiteralPath 'C:\Temp\Sql' -Recurse -Force -ErrorAction SilentlyContinue
		}
		""";
}
