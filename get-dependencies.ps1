function Is-Admin() {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function main() {
    if (-not (Is-Admin)) {
        Write-Host "error: administrator privileges required"
        return 1
    }

    if (Test-Path ".\tmp\") {
        Remove-Item -Path ".\tmp\" -Recurse -Force
    }

    mkdir ".\tmp\"

    $urls = @{
        "NVIDIA_Inspector" = "https://ftp.nluug.nl/pub/games/PC/guru3d/tweak/nvidiaInspector-[Guru3D.com].zip"
    }

    # ======================
    # Setup NVIDIA Inspector
    # ======================

    # download and extract inspector
    Invoke-WebRequest $urls["NVIDIA_Inspector"] -OutFile ".\tmp\NVIDIA-Inspector.zip"
    Expand-Archive -Path ".\tmp\NVIDIA-Inspector.zip" -DestinationPath ".\tmp\NVIDIA-Inspector\"

    # move inspector to limit-nvpstate project directory
    Copy-Item ".\tmp\NVIDIA-Inspector\nvidiaInspector.exe" ".\limit-nvpstate\limit-nvpstate\"

    return 0
}

$_exitCode = main
Write-Host # new line
exit $_exitCode
