<#
    把本项目编译所需的 NuGet 包下载到仓库内的 packages\ 目录，实现"局部环境"：
    不安装 Visual Studio、不安装 .NET Framework Targeting Pack、不写任何全局位置。
    packages\ 已被 .gitignore 忽略，不会进入版本库。

    用法（Windows PowerShell 一定可用；装了 PowerShell 7 的话把 powershell 换成 pwsh 亦可）：
        powershell -ExecutionPolicy Bypass -File tools\setup-build-env.ps1

    包清单：
        Newtonsoft.Json 11.0.2      运行时依赖（见 Mo3ModManager\packages.config）
        SharpCompress 0.23.0        运行时依赖（见 Mo3ModManager\packages.config）
        Microsoft.Net.Compilers 3.11.0
                                    Roslyn C# 编译器。系统自带的 MSBuild 4.0 只含
                                    C# 5 编译器，而本项目使用了 C# 6+ 语法
                                    （out var、?. 等），必须用这个包里的 csc.exe。
        Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3
                                    .NET Framework 4.8 的"引用程序集"（即免安装版的
                                    Targeting Pack）。缺了它 MSBuild 会回退到运行时目录
                                    里的程序集，除了报 MSB3644 警告，还会把 mscorlib.dll、
                                    norm*.nlp、zh-Hans 等一并复制进 bin 输出目录。
#>
[CmdletBinding()]
param(
    [string] $PackagesDirectory
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if (-not $PackagesDirectory) { $PackagesDirectory = Join-Path $root 'packages' }

$packages = @(
    @{ Id = 'newtonsoft.json';        Version = '11.0.2'; Directory = 'Newtonsoft.Json.11.0.2' },
    @{ Id = 'sharpcompress';          Version = '0.23.0'; Directory = 'SharpCompress.0.23.0' },
    @{ Id = 'microsoft.net.compilers'; Version = '3.11.0'; Directory = 'Microsoft.Net.Compilers.3.11.0' },
    @{ Id = 'microsoft.netframework.referenceassemblies.net48'; Version = '1.0.3'; Directory = 'Microsoft.NETFramework.ReferenceAssemblies.net48.1.0.3' }
)

New-Item -ItemType Directory -Force -Path $PackagesDirectory | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Save-Nupkg {
    param([string] $Url, [string] $Destination)

    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing -ErrorAction Stop
        return
    } catch {
        Write-Verbose "Invoke-WebRequest 失败：$($_.Exception.Message)"
    }

    # 某些受限环境（受限令牌/沙箱）下 .NET 的 schannel 无法建立 TLS 客户端凭证
    # （报 SEC_E_NO_CREDENTIALS 或"基础连接已经关闭"），此时改用 Node.js 自带的
    # OpenSSL 来下载，它不依赖 Windows 证书存储。
    $node = Get-Command node.exe -ErrorAction SilentlyContinue
    if (-not $node) {
        throw "无法下载 $Url：Invoke-WebRequest 失败，且环境中没有 node.exe 可作为备选下载器。"
    }

    $scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) 'mo3-fetch-nupkg.js'
    @'
const https = require("https");
const fs = require("fs");
// nuget.org 的 flatcontainer 端点会 302 重定向到 CDN，必须跟随重定向，
// 否则会把重定向的响应体当成 nupkg 写进文件（解压时报 "Central Directory corrupt"）。
function fetch(url, dest, redirectsLeft) {
  https.get(url, { headers: { "user-agent": "mo3-build-setup" } }, (res) => {
    if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
      res.resume();
      if (redirectsLeft <= 0) { console.error("too many redirects"); process.exit(1); }
      return fetch(new URL(res.headers.location, url).toString(), dest, redirectsLeft - 1);
    }
    if (res.statusCode !== 200) { console.error("HTTP " + res.statusCode); process.exit(1); }
    const file = fs.createWriteStream(dest);
    res.pipe(file);
    file.on("finish", () => file.close(() => console.log("downloaded " + fs.statSync(dest).size + " bytes")));
  }).on("error", (e) => { console.error("ERR " + e.message); process.exit(1); });
}
fetch(process.argv[2], process.argv[3], 5);
'@ | Set-Content -Path $scriptPath -Encoding UTF8

    # --use-system-ca 需要 Node 22+；老版本 Node 不认识该参数，退回普通调用。
    & $node.Source '--use-system-ca' $scriptPath $Url $Destination
    if ($LASTEXITCODE -ne 0) {
        & $node.Source $scriptPath $Url $Destination
    }
    if ($LASTEXITCODE -ne 0) { throw "下载失败：$Url" }
}

foreach ($p in $packages) {
    $target = Join-Path $PackagesDirectory $p.Directory
    if (Test-Path $target) {
        Write-Host "已存在，跳过：$($p.Directory)"
        continue
    }

    $url = "https://api.nuget.org/v3-flatcontainer/$($p.Id)/$($p.Version)/$($p.Id).$($p.Version).nupkg"
    $tempNupkg = Join-Path ([System.IO.Path]::GetTempPath()) "$($p.Id).$($p.Version).nupkg"
    Write-Host "下载 $($p.Id) $($p.Version) ..."
    Save-Nupkg -Url $url -Destination $tempNupkg

    [System.IO.Compression.ZipFile]::ExtractToDirectory($tempNupkg, $target)
    # 按 packages.config 风格的 NuGet 目录约定，把 .nupkg 本身也留在包目录里。
    Copy-Item $tempNupkg (Join-Path $target "$($p.Directory).nupkg") -Force
    Remove-Item $tempNupkg -Force
    Write-Host "已安装：$($p.Directory)"
}

Write-Host ''
Write-Host "依赖已就绪：$PackagesDirectory"
