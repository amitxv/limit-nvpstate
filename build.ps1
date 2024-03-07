function Is-Admin() {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function main() {
    if (-not (Is-Admin)) {
        Write-Host "error: administrator privileges required"
        return 1
    }

    # build application
    MSBuild.exe ".\limit-nvpstate\limit-nvpstate.sln" -p:Configuration=Release -p:Platform=x64

    if (Test-Path ".\build\") {
        Remove-Item -Path ".\build\" -Recurse -Force
    }

    # create folder structure
    New-Item -ItemType Directory -Path ".\build\limit-nvpstate\"

    # create final package
    Copy-Item ".\limit-nvpstate\limit-nvpstate\bin\x64\Release\limit-nvpstate.exe" ".\build\limit-nvpstate\"
    Copy-Item ".\limit-nvpstate\limit-nvpstate\bin\x64\Release\nvidiaInspector.exe" ".\build\limit-nvpstate\"

    return 0
}

$_exitCode = main
Write-Host # new line
exit $_exitCode
