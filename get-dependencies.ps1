function main() {
    if (Test-Path ".\tmp\") {
        Remove-Item -Path ".\tmp\" -Recurse -Force
    }

    mkdir ".\tmp\"

    $urls = @{
        "NVIDIA_Inspector" = "https://us4-dl.techpowerup.com/files/qnZzgPNz_bk-mlqbF5hUVw/1709898622/NVIDIA_Inspector_1.9.8.7_Beta.zip"
    }

    # =============
    # Setup NVIDIA Inspector
    # =============
    Invoke-WebRequest $urls["NVIDIA_Inspector"] -OutFile ".\tmp\NVIDIA-Inspector.zip"

    # extract zip file contents
    Expand-Archive -Path ".\tmp\NVIDIA-Inspector.zip" -DestinationPath ".\tmp\NVIDIA-Inspector\"

    Copy-Item ".\tmp\NVIDIA-Inspector\nvidiaInspector.exe" ".\limit-nvpstate\limit-nvpstate\"

    return 0
}

$_exitCode = main
Write-Host # new line
exit $_exitCode
