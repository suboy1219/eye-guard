param(
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src"
$out = Join-Path $root "eye-guard.exe"
$ico = Join-Path $root "app.ico"

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    throw "未找到 .NET Framework 4.x 编译器 csc.exe"
}

if ($Clean) {
    Remove-Item -LiteralPath $out -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $ico -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $ico)) {
    & (Join-Path $root "make-icon.ps1")
}

$sources = Get-ChildItem -LiteralPath $src -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/out:$out",
    "/win32icon:$ico",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Web.Extensions.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll"
) + $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出码 $LASTEXITCODE"
}

$info = Get-Item -LiteralPath $out
Write-Host ("编译成功: {0} ({1:N0} bytes)" -f $info.FullName, $info.Length)
