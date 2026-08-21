$ErrorActionPreference = "Stop"

$root = $PSScriptRoot

$adminSource = Join-Path $root "clients\admin"
$distPath = Join-Path $adminSource "dist"
$deployPath = Join-Path $root "deploy\admin"
$zipPath = Join-Path $root "deploy\admin.zip"

Write-Host "Building Admin..."

Push-Location $adminSource

try {
    npm run build

    if ($LASTEXITCODE -ne 0) {
        throw "Admin build failed."
    }
}
finally {
    Pop-Location
}

Write-Host "Preparing deployment folder..."

if (Test-Path $deployPath) {
    Remove-Item "$deployPath\*" -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $deployPath | Out-Null
}

Get-ChildItem $distPath -Force |
    Where-Object { $_.Name -ne "config.json" } |
    Copy-Item -Destination $deployPath -Recurse -Force

if (Test-Path (Join-Path $deployPath "config.json")) {
    throw "ERROR: config.json must not be included in deployment."
}

Write-Host "Creating admin.zip..."

Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

Compress-Archive `
    -Path "$deployPath\*" `
    -DestinationPath $zipPath `
    -CompressionLevel Optimal

Write-Host ""
Write-Host "Admin deployment package created successfully:"
Write-Host $zipPath
Write-Host ""
Write-Host "Production config.json was NOT included."