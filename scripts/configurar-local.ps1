param(
    [string]$Servidor = 'oracle.fiap.com.br',
    [int]$Porta = 1521,
    [string]$Servico = 'ORCL'
)
$ErrorActionPreference = 'Stop'
$encodingAnterior = $OutputEncoding
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $usuarioBanco = Read-Host 'Usuário Oracle (RM)'
    $senhaSegura = Read-Host 'Senha Oracle (fica oculta)' -AsSecureString
    $senhaBanco = [System.Net.NetworkCredential]::new('', $senhaSegura).Password
    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    # evita o erro de conversão do windows powershell
    $connection.set_Item('User Id', [string]$usuarioBanco)
    $connection.set_Item('Password', [string]$senhaBanco)
    $connection.set_Item('Data Source', [string]"${Servidor}:$Porta/$Servico")
    $connection.set_Item('Connection Timeout', [int]15)
    $jwtBytes = New-Object byte[] 48
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($jwtBytes) } finally { $rng.Dispose() }
    $jwtKey = [Convert]::ToBase64String($jwtBytes)
    @{
        'ConnectionStrings:BairrOnOracle' = $connection.ConnectionString
        'Jwt:Key' = $jwtKey
    } | ConvertTo-Json -Compress | dotnet user-secrets set --project .\BairrOn.Api.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível salvar a configuração local.' }
    Write-Host 'Configuração salva no User Secrets, fora do projeto. Nenhuma tabela foi alterada.'
} finally {
    $senhaBanco = $null; $senhaSegura = $null; $connection = $null; $jwtKey = $null; $jwtBytes = $null
    $OutputEncoding = $encodingAnterior
    Pop-Location
}
