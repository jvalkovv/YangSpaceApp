# Shared manual QA process runner — Windows PowerShell 5.1 compatible.
# Uses an explicit .NET Process handle (not Start-Process -PassThru/-Wait),
# with a temporary, logged CMD launcher for both .exe and npm.cmd.
# All command arguments are internal QA constants; untrusted shell syntax
# is rejected, and no secrets must be provided as command arguments.
function Invoke-YangSpaceStage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Executable,
        [Parameter(Mandatory=$true)][string[]]$CommandArguments,
        [Parameter(Mandatory=$true)][string]$WorkingDirectory,
        [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
        [ValidateRange(1,3600)][int]$TimeoutSeconds = 600
    )
    if ($Label -cnotmatch '^[a-z][a-z0-9-]*$') { throw 'STAGE_LABEL_INVALID' }
    if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) { throw ('STAGE_EXECUTABLE_MISSING=' + $Label) }
    if (-not (Test-Path -LiteralPath $WorkingDirectory -PathType Container)) { throw ('STAGE_WORKDIR_MISSING=' + $Label) }
    if (-not (Test-Path -LiteralPath $EvidenceDirectory -PathType Container)) { throw ('STAGE_EVIDENCE_DIR_MISSING=' + $Label) }
    # The CMD launcher must never interpolate user-controlled expansion syntax.
    foreach ($value in @($Executable, $WorkingDirectory, $EvidenceDirectory)) {
        if ($value -match '[\r\n"%]') { throw ('STAGE_UNSAFE_PATH=' + $Label) }
    }
    foreach ($argument in $CommandArguments) {
        # No shell operators, expansion, quotes or newlines in arguments.
        if ($argument -match '[\r\n"%!^&|<>]') { throw ('STAGE_UNSAFE_ARGUMENT=' + $Label) }
    }
    $commandName = Join-Path $EvidenceDirectory ($Label + '.runner.cmd')
    $out = Join-Path $EvidenceDirectory ($Label + '.stdout.log')
    $err = Join-Path $EvidenceDirectory ($Label + '.stderr.log')
    $cmd = [Environment]::GetEnvironmentVariable('ComSpec', 'Process')
    if (-not $cmd -or -not (Test-Path -LiteralPath $cmd -PathType Leaf)) {
        throw 'WINDOWS_COMMAND_INTERPRETER_MISSING'
    }
    $quoted = @($CommandArguments | ForEach-Object {
        if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
    })
    $commandLine = '"' + $Executable + '" ' + ($quoted -join ' ') + ' 1>"' + $out + '" 2>"' + $err + '"'
    $lines = @(
        '@echo off'
        'setlocal DisableDelayedExpansion'
        $commandLine
        'exit /b %errorlevel%'
    )
    [System.IO.File]::WriteAllLines($commandName, $lines, [System.Text.Encoding]::Default)

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $cmd
    $info.Arguments = '/d /s /c ""' + $commandName + '""'
    $info.WorkingDirectory = $WorkingDirectory
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $info
    Write-Host ('STAGE=' + $Label)
    try {
        if (-not $process.Start()) { throw ('STAGE_START_FAILED=' + $Label) }
        $finished = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $finished) {
            $pidToStop = $process.Id
            try {
                # Only kill the launcher started by this function.
                $process.Kill()
                [void]$process.WaitForExit(5000)
            } catch {
                Write-Warning ('STAGE_LAUNCHER_CLEANUP_FAILED=' + $pidToStop)
            }
            if (Test-Path $err) { Get-Content $err -Tail 25 }
            if (Test-Path $out) { Get-Content $out -Tail 25 }
            throw ('STAGE_TIMEOUT=' + $Label + ' PID=' + $pidToStop)
        }

        $process.Refresh()
        # A Process that we create and start ourselves exposes a real
        # integer ExitCode (unlike the nullable Start-Process passthrough
        # observed in Windows PowerShell in the previous wrapper).
        $exitCode = [int]$process.ExitCode
        if ($exitCode -ne 0) {
            if (Test-Path $err) { Get-Content $err -Tail 30 }
            if (Test-Path $out) { Get-Content $out -Tail 30 }
            throw ('STAGE_FAILED=' + $Label + ' EXIT=' + $exitCode)
        }
        Write-Host ('STAGE_PASS=' + $Label)
    } finally {
        $process.Dispose()
    }
}
