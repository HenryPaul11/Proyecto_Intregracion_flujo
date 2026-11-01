param(
    [Parameter(Mandatory=$true)][string]$File,
    [string]$Server = 'localhost',
    [string]$Database = 'sakila'
)
$ErrorActionPreference = 'Stop'
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$builder['Data Source'] = $Server
$builder['Initial Catalog'] = $Database
$builder['Integrated Security'] = $true
$builder['TrustServerCertificate'] = $true
$connection = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = Get-Content -LiteralPath $File -Raw -Encoding UTF8
    $command.CommandTimeout = 60
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
    $results = New-Object System.Data.DataSet
    [void]$adapter.Fill($results)
    foreach ($table in $results.Tables) { $table | Format-Table -AutoSize }
    Write-Output 'SQL ejecutado correctamente.'
} finally { $connection.Dispose() }
