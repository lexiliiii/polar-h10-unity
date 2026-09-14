$ErrorActionPreference = 'Stop'
$bridge = Join-Path $PSScriptRoot 'PolarH10Bridge'
$web = Join-Path $PSScriptRoot 'PolarH10Visualizer'
$cache = Join-Path $PSScriptRoot '.npm-cache'
if (-not (Test-Path (Join-Path $bridge 'node_modules/ws/package.json'))) {
    & npm.cmd ci --prefix $bridge --cache $cache --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Bridge dependency installation failed.' }
}
if (-not (Test-Path (Join-Path $web 'node_modules/esbuild/package.json'))) {
    & npm.cmd ci --prefix $web --cache $cache --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Visualizer dependency installation failed.' }
}
& node (Join-Path $bridge 'build-web.mjs')
if ($LASTEXITCODE -ne 0) { throw 'Visualizer build failed.' }
& node (Join-Path $bridge 'server.mjs')
