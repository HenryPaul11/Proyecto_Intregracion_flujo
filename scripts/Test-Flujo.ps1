param(
    [string]$BaseUrl = 'http://localhost:5236',
    [string]$AccountsFile = "$PSScriptRoot/../ProyectoU1/.local/cuentas-desarrollo.json"
)
$ErrorActionPreference = 'Stop'
$accounts = Get-Content -LiteralPath $AccountsFile -Raw -Encoding UTF8 | ConvertFrom-Json
$checks = 0
function Assert-Flow($condition, [string]$message) {
    if (!$condition) { throw "FALLO: $message" }
    $script:checks++
    Write-Output "OK: $message"
}
function Get-Page($session, [string]$path) {
    Invoke-WebRequest "$BaseUrl$path" -WebSession $session -UseBasicParsing
}
function Send-Form($session, [string]$pagePath, [string]$action, [hashtable]$values) {
    $page = Get-Page $session $pagePath
    $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value
    if (!$token) { throw "Falta token CSRF en $pagePath" }
    $values['__RequestVerificationToken'] = $token
    Invoke-WebRequest "$BaseUrl$action" -Method Post -WebSession $session -Body $values -UseBasicParsing
}
function Connect-Account($account) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $result = Send-Form $session '/Cuenta/Login' '/Cuenta/Login' @{ Login=$account.Usuario; Password=$account.Password }
    if ($result.Content -notmatch 'Cerrar sesi') { throw 'No se pudo iniciar sesion' }
    return $session
}
function Get-Status($session, [string]$path) {
    try { return (Get-Page $session $path).StatusCode }
    catch { if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }; throw }
}

$admin = Connect-Account $accounts[0]
$client = Connect-Account $accounts[1]
$other = Connect-Account $accounts[2]
Assert-Flow ((Get-Status $client '/Peliculas/Administrar') -eq 403) 'Cliente bloqueado en URL administrativa'
Assert-Flow ((Get-Status $client '/Peliculas/Crear') -eq 403) 'Cliente bloqueado al crear peliculas'
Assert-Flow ((Get-Status $client '/Reportes/PeliculasMasAlquiladas') -eq 403) 'Reporte restringido al administrador'
$anonymous = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$page = Get-Page $anonymous '/Peliculas'
Assert-Flow ($page.BaseResponse.ResponseUri.AbsolutePath -eq '/Cuenta/Login') 'Anonimo redirigido al login'
$page = Get-Page $client '/Peliculas'
Assert-Flow ($page.Content -match 'ACADEMY DINOSAUR') 'Catalogo lee las peliculas de Sakila'
Assert-Flow ($page.Content -notmatch '>Administraci') 'Menu del cliente oculta administracion'

$filmData = @{Titulo=''; Descripcion='Prueba automatizada del flujo real'; Anio=2026; IdiomaId=1; DuracionAlquiler=3; Tarifa='4.99'; Duracion=95; CostoReposicion='19.99'; Clasificacion='PG'}
$invalid = Send-Form $admin '/Peliculas/Crear' '/Peliculas/Guardar' $filmData
Assert-Flow ($invalid.Content -match 'validation-summary-errors') 'Datos invalidos rechazados por el servidor'
$title = 'PRUEBA FLUJO ' + [Guid]::NewGuid().ToString('N').Substring(0,8).ToUpperInvariant()
$filmData.Titulo = $title
$created = Send-Form $admin '/Peliculas/Crear' '/Peliculas/Guardar' $filmData
$filmId = [regex]::Match($created.BaseResponse.ResponseUri.AbsoluteUri, '/Detalle(?:/|\?id=)(\d+)').Groups[1].Value
Assert-Flow ([bool]$filmId) 'Pelicula creada mediante formulario MVC'
$filmData['Id'] = $filmId
$filmData.Descripcion = 'Descripcion editada en prueba de integracion'
$edited = Send-Form $admin "/Peliculas/Editar/$filmId" '/Peliculas/Guardar' $filmData
Assert-Flow ($edited.Content -match 'Descripcion editada') 'Edicion persistida en SQL Server'
$copyPage = Send-Form $admin "/Peliculas/Inventario/$filmId" '/Peliculas/AgregarCopia' @{id=$filmId;tiendaId=1}
$copyId = [regex]::Match($copyPage.Content, 'name="copiaId" value="(\d+)"').Groups[1].Value
Assert-Flow ([bool]$copyId) 'Copia registrada en inventario'

try {
    Invoke-WebRequest "$BaseUrl/Alquileres/Alquilar" -Method Post -WebSession $client -Body @{peliculaId=$filmId} -UseBasicParsing | Out-Null
    throw 'La operacion acepto una peticion sin CSRF'
} catch {
    Assert-Flow ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 400) 'Operacion sin token CSRF rechazada'
}
$rented = Send-Form $client "/Peliculas/Detalle/$filmId" '/Alquileres/Alquilar' @{peliculaId=$filmId; customerId=2}
Assert-Flow ($rented.Content -match 'Alquiler registrado') 'Alquiler asociado al cliente de Session, ignorando customerId del formulario'
$rentalId = [regex]::Match($rented.Content, 'name="id" value="(\d+)"').Groups[1].Value
Assert-Flow ([bool]$rentalId) 'Alquiler propio visible en Mis alquileres'
$otherList = Get-Page $other '/Alquileres'
Assert-Flow ($otherList.Content -notmatch $title) 'Otro cliente no visualiza el alquiler'
$noStock = Send-Form $other '/Alquileres' '/Alquileres/Alquilar' @{peliculaId=$filmId}
Assert-Flow ($noStock.Content -match 'No hay copias disponibles') 'Segunda solicitud rechazada al agotarse la unica copia'
$removeBusy = Send-Form $admin "/Peliculas/Inventario/$filmId" '/Peliculas/RetirarCopia' @{id=$filmId;copiaId=$copyId}
Assert-Flow ($removeBusy.Content -match 'No se puede retirar') 'No se permite retirar una copia alquilada'
$foreignReturn = Send-Form $other '/Alquileres' '/Alquileres/Devolver' @{id=$rentalId}
Assert-Flow ($foreignReturn.Content -match 'no te pertenece') 'Devolucion ajena bloqueada'
$returned = Send-Form $client '/Alquileres' '/Alquileres/Devolver' @{id=$rentalId}
Assert-Flow ($returned.Content -match 'Se conserva el historial') 'Devolucion propia registrada sin borrar el historial'
$again = Send-Form $client '/Alquileres' '/Alquileres/Devolver' @{id=$rentalId}
Assert-Flow ($again.Content -match 'ya fue devuelto') 'Segunda devolucion rechazada'
$removed = Send-Form $admin "/Peliculas/Inventario/$filmId" '/Peliculas/RetirarCopia' @{id=$filmId;copiaId=$copyId}
Assert-Flow ($removed.Content -match 'Retirada') 'Copia devuelta retirada mediante baja logica'
$deactivated = Send-Form $admin '/Peliculas/Administrar' '/Peliculas/CambiarEstado' @{id=$filmId;activo='false'}
$search = Get-Page $client "/Peliculas?buscar=$([uri]::EscapeDataString($title))"
Assert-Flow ($search.Content -match 'No se encontraron') 'Pelicula inactiva excluida del catalogo'
Assert-Flow ((Get-Status $client "/Peliculas/Detalle/$filmId") -eq 404) 'Cliente no accede al detalle de pelicula inactiva'
$history = Get-Page $client '/Alquileres'
Assert-Flow ($history.Content -match $title) 'Historial conservado tras desactivar la pelicula'
$report = Get-Page $admin '/Reportes/PeliculasMasAlquiladas'
Assert-Flow ($report.Content -match 'BUCKET BROTHERHOOD') 'Reporte ejecuta el ranking real de Sakila'
$logout = Send-Form $client '/Alquileres' '/Cuenta/Logout' @{}
$after = Get-Page $client '/Alquileres'
Assert-Flow ($after.BaseResponse.ResponseUri.AbsolutePath -eq '/Cuenta/Login') 'Cerrar sesion elimina el acceso autenticado'
Write-Output "Resultado: $checks comprobaciones correctas. Pelicula de prueba $filmId desactivada; historial conservado."
