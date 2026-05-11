param(
    [Parameter(Mandatory = $true)]
    [string]$StudentRegistrarConnectionString,

    [Parameter(Mandatory = $true)]
    [string]$KeycloakUrl,

    [string]$KeycloakAuthority,

    [string]$ApiUrls = 'http://127.0.0.1:0',

    [string]$ProjectPath = 'src/StudentRegistrar.Api/StudentRegistrar.Api.csproj',

    [string]$BaseOutputPath = 'artifacts/validation/migration-verify/',

    [int]$StartupTimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($KeycloakAuthority)) {
    $KeycloakAuthority = $KeycloakUrl
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$resolvedProjectPath = (Resolve-Path (Join-Path $repoRoot $ProjectPath)).Path
$resolvedBaseOutputPath = (Join-Path $repoRoot $BaseOutputPath).TrimEnd([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))

New-Item -ItemType Directory -Path $resolvedBaseOutputPath -Force | Out-Null

$arguments = @(
    'run'
    '--project'
    $resolvedProjectPath
    '--no-launch-profile'
    "-p:BaseOutputPath=$resolvedBaseOutputPath"
)

$standardOutputLog = Join-Path $resolvedBaseOutputPath 'verify-api-migrations.stdout.log'
$standardErrorLog = Join-Path $resolvedBaseOutputPath 'verify-api-migrations.stderr.log'

Set-Content -Path $standardOutputLog -Value $null
Set-Content -Path $standardErrorLog -Value $null

$quotedArguments = ($arguments | ForEach-Object { '"{0}"' -f ($_ -replace '"', '\"') }) -join ' '
$commandText = @(
    ('set "ASPNETCORE_ENVIRONMENT=Development"')
    ('set "ASPNETCORE_URLS={0}"' -f $ApiUrls)
    ('set "ConnectionStrings__studentregistrar={0}"' -f $StudentRegistrarConnectionString)
    ('set "ConnectionStrings__keycloak={0}"' -f $KeycloakUrl)
    ('set "Keycloak__Authority={0}"' -f $KeycloakAuthority)
    ('dotnet {0} 1>"{1}" 2>"{2}"' -f $quotedArguments, $standardOutputLog, $standardErrorLog)
) -join ' && '

$process = $null

try {
    Write-Host 'Starting API migration verification with isolated output path and no launch profile...'
    Write-Host "Project: $resolvedProjectPath"
    Write-Host "BaseOutputPath: $resolvedBaseOutputPath"
    Write-Host "ASPNETCORE_URLS: $ApiUrls"

    $process = Start-Process -FilePath 'cmd.exe' -ArgumentList '/d', '/c', $commandText -WorkingDirectory $repoRoot -WindowStyle Hidden -PassThru

    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    $successPatterns = @(
        'Application started. Press Ctrl+C to shut down.',
        'Now listening on:'
    )
    $failurePatterns = @(
        'Unhandled exception.',
        'Build FAILED.',
        'Unable to connect',
        'Address already in use'
    )

    $lastOutput = ''
    $lastError = ''

    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) {
            $combinedOutput = (Get-Content -Path $standardOutputLog -Raw) + [Environment]::NewLine + (Get-Content -Path $standardErrorLog -Raw)
            if ($process.ExitCode -eq 0 -and ($successPatterns | Where-Object { $combinedOutput.Contains($_) })) {
                Write-Host $combinedOutput.Trim()
                Write-Host 'Migration verification succeeded. API exited cleanly.'
                return
            }

            throw "API exited before startup verification completed. Exit code: $($process.ExitCode)`n$combinedOutput"
        }

        $currentOutput = Get-Content -Path $standardOutputLog -Raw
        $currentError = Get-Content -Path $standardErrorLog -Raw
        $combinedOutput = $currentOutput + [Environment]::NewLine + $currentError

        if ($currentOutput -ne $lastOutput -or $currentError -ne $lastError) {
            if (-not [string]::IsNullOrWhiteSpace($currentOutput)) {
                Write-Host $currentOutput.TrimEnd()
            }
            if (-not [string]::IsNullOrWhiteSpace($currentError)) {
                Write-Host $currentError.TrimEnd()
            }

            $lastOutput = $currentOutput
            $lastError = $currentError
        }

        if ($successPatterns | Where-Object { $combinedOutput.Contains($_) }) {
            Write-Host 'Migration verification succeeded. Stopping the API process.'
            return
        }

        if ($failurePatterns | Where-Object { $combinedOutput.Contains($_) }) {
            throw "Migration verification failed.`n$combinedOutput"
        }

        Start-Sleep -Seconds 1
    }

    $timedOutOutput = (Get-Content -Path $standardOutputLog -Raw) + [Environment]::NewLine + (Get-Content -Path $standardErrorLog -Raw)
    throw "Timed out waiting for API startup confirmation after $StartupTimeoutSeconds seconds.`n$timedOutOutput"
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }

    if ($null -ne $process) {
        $process.Dispose()
    }
}