param(
    [Parameter(Mandatory = $true)]
    [string]$UnityEditorPath,

    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$LogFile,

    [Parameter(Mandatory = $true)]
    [string]$ExecuteMethod,

    [string]$Version
)

Set-StrictMode -Version Latest

$unityExecutable = Join-Path $UnityEditorPath "Unity.exe"
if (-not (Test-Path -LiteralPath $unityExecutable -PathType Leaf))
{
    [Console]::Error.WriteLine("Unity executable does not exist: $unityExecutable")
    exit 1
}

$requiredProjectDirectories = @("Assets", "Packages", "ProjectSettings")
foreach ($directory in $requiredProjectDirectories)
{
    $requiredPath = Join-Path $ProjectPath $directory
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Container))
    {
        [Console]::Error.WriteLine("Invalid Unity project path, missing ${directory}: $ProjectPath")
        exit 1
    }
}

$absoluteLogFile = if ([IO.Path]::IsPathRooted($LogFile))
{
    [IO.Path]::GetFullPath($LogFile)
}
else
{
    [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $LogFile))
}

$logDirectory = Split-Path -Parent $absoluteLogFile
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null

$unityArguments = @(
    "-projectPath", $ProjectPath,
    "-batchmode",
    "-quit",
    "-logFile", "-",
    "-executeMethod", $ExecuteMethod
)

if (-not [string]::IsNullOrWhiteSpace($Version))
{
    $unityArguments += "-version=$Version"
}

$unityArguments += "-CustomArgs:Language=en_US;$ProjectPath"

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
$logWriter = New-Object System.IO.StreamWriter($absoluteLogFile, $false, $utf8WithoutBom)
$exitCode = 1

try
{
    & $unityExecutable @unityArguments 2>&1 | ForEach-Object {
        $line = $_.ToString()
        [Console]::Out.WriteLine($line)
        $logWriter.WriteLine($line)
        $logWriter.Flush()
    }
    $exitCode = $LASTEXITCODE
}
catch
{
    $message = "Unity launch failed: $($_.Exception.Message)"
    [Console]::Error.WriteLine($message)
    $logWriter.WriteLine($message)
    $logWriter.Flush()
}
finally
{
    $logWriter.Dispose()
}

exit $exitCode
