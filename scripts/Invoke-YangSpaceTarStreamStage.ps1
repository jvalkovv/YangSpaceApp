# Windows PowerShell 5.1: binary-safe, one-way host TAR -> docker exec stdin.
# Docker cp cannot write to --read-only container rootfs even when /tmp is tmpfs.
# This helper preserves read-only rootfs and writes only to an approved sidecar tmpfs.
# Existing callers retain /tmp/qa; the isolated PG bearer QA uses /qa so
# /tmp stays noexec while packaged native libraries can be memory mapped.
# Never use PowerShell's byte pipeline: it transforms binary data.
function Invoke-YangSpaceTarStreamStage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string]$Docker,
        [Parameter(Mandatory=$true)][string]$Container,
        [Parameter(Mandatory=$true)][string]$Archive,
        [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
        [ValidateSet('/tmp/qa','/qa')][string]$Destination = '/tmp/qa',
        [ValidateRange(1,1800)][int]$TimeoutSeconds = 600
    )
    if ($Container -cnotmatch '^yangspace-f2a3-appqa-[a-f0-9]{12}$') {
        throw 'F2A3_TAR_CONTAINER_ID_REFUSED'
    }
    if (!(Test-Path -LiteralPath $Docker -PathType Leaf) -or
        !(Test-Path -LiteralPath $Archive -PathType Leaf) -or
        !(Test-Path -LiteralPath $EvidenceDirectory -PathType Container)) {
        throw 'F2A3_TAR_INPUT_MISSING'
    }
    if ((Get-Item -LiteralPath $Archive).Length -lt 1024) {
        throw 'F2A3_TAR_EMPTY_ARCHIVE'
    }

    $Label = 'pgapp-stream-runtime'
    $CmdPath = Join-Path $EvidenceDirectory ($Label + '.runner.cmd')
    $StdoutPath = Join-Path $EvidenceDirectory ($Label + '.stdout.log')
    $StderrPath = Join-Path $EvidenceDirectory ($Label + '.stderr.log')

    # This is a receipt, NOT an executable replay of the binary transfer.
    [IO.File]::WriteAllLines($CmdPath, [string[]]@(
        '@echo off',
        'REM Binary input was streamed directly by System.Diagnostics.Process.',
        ('REM Recipient: docker exec -i CONTAINER sh -c "mkdir -p ' + $Destination + ' && tar -C ' + $Destination + ' -xf -"'),
        'REM This receipt is intentionally non-replayable without verified input.',
        'exit /b 1'
    ))
    $StdoutText = ''
    $StderrText = ''
    $ExitCode = -1
    $Started = $false
    $Succeeded = $false
    $process = New-Object System.Diagnostics.Process
    $archiveStream = $null
    try {
        $info = New-Object System.Diagnostics.ProcessStartInfo
        $info.FileName = $Docker
        $info.Arguments = 'exec -i ' + $Container +
            ' sh -c "mkdir -p ' + $Destination + ' && tar -C ' + $Destination + ' -xf -"'
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardInput = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $process.StartInfo = $info

        Write-Host ('STAGE=' + $Label)
        if (!$process.Start()) { throw 'F2A3_TAR_DOCKER_EXEC_START_FAILED' }
        $Started = $true
        $outTask = $process.StandardOutput.ReadToEndAsync()
        $errTask = $process.StandardError.ReadToEndAsync()
        $archiveStream = [IO.File]::OpenRead($Archive)

        # Stream bytes unchanged, never through a PowerShell object pipeline.
        $copyTask = $archiveStream.CopyToAsync(
            $process.StandardInput.BaseStream
        )
        if (!$copyTask.Wait($TimeoutSeconds * 1000)) {
            throw 'F2A3_TAR_STREAM_TIMEOUT'
        }
        $copyTask.GetAwaiter().GetResult()
        $process.StandardInput.Close()

        if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
            throw 'F2A3_TAR_RECEIVER_TIMEOUT'
        }
        $ExitCode = [int]$process.ExitCode
        $StdoutText = $outTask.GetAwaiter().GetResult()
        $StderrText = $errTask.GetAwaiter().GetResult()
        if ($ExitCode -ne 0) {
            throw ('F2A3_TAR_STREAM_EXIT=' + $ExitCode)
        }
        $Succeeded = $true
    }
    finally {
        if ($archiveStream) { $archiveStream.Dispose() }
        if ($Started -and !$process.HasExited) {
            try {
                $process.Kill()
                [void]$process.WaitForExit(5000)
            }
            catch {
                Write-Warning 'F2A3_TAR_STREAM_LAUNCHER_CLEANUP_FAILED'
            }
        }
        $process.Dispose()
        [IO.File]::WriteAllText($StdoutPath, $StdoutText)
        [IO.File]::WriteAllText($StderrPath, $StderrText)
    }
    if (!$Succeeded) { throw 'F2A3_TAR_STREAM_INCOMPLETE' }
    Write-Host ('STAGE_PASS=' + $Label)
}