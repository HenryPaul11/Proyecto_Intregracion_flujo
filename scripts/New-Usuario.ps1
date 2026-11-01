param(
    [Parameter(Mandatory=$true)][string]$Login,
    [Parameter(Mandatory=$true)][string]$Nombre,
    [Parameter(Mandatory=$true)][ValidateSet('Cliente', 'Administrador')][string]$Rol,
    [Parameter(Mandatory=$true)][ValidateRange(1, 2147483647)][int]$IdSakila
)
$ErrorActionPreference = 'Stop'
$password = Read-Host 'Contrasena de 12 a 128 caracteres (no se muestra)' -AsSecureString
$previousPassword = $env:SAKILA_SETUP_PASSWORD
try {
    $env:SAKILA_SETUP_PASSWORD = (New-Object System.Net.NetworkCredential('', $password)).Password
    Push-Location "$PSScriptRoot/../ProyectoU1"
    try {
        dotnet run --no-launch-profile -- --crear-usuario $Login $Nombre $Rol $IdSakila
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el usuario. Revise el login, la relacion Sakila y la conexion.' }
    } finally { Pop-Location }
} finally {
    $env:SAKILA_SETUP_PASSWORD = $previousPassword
    $password.Dispose()
}
