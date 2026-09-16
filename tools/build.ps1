<#
    构建本项目，只使用仓库内 packages\ 里的局部依赖 + 系统自带的 .NET Framework
    MSBuild，不需要安装 Visual Studio 或任何全局 SDK。

    用法：
        build.cmd                      生成 Debug 版本（普通编译）
        build.cmd Release              生成 Release 版本（普通编译）
        build.cmd -Clean               清空缓存后生成 Debug（先删 bin\ 和 obj\）
        build.cmd -Clean Release       清空缓存后生成 Release
        powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Configuration Release -Clean

    首次使用（或 packages\ 被清理后）先运行：
        powershell -ExecutionPolicy Bypass -File tools\setup-build-env.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [string] $Platform = 'Any CPU',
    [switch] $Clean
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Mo3ModManager.sln'
$packagesDir = Join-Path $root 'packages'

# -Clean：删掉 bin\ 与 obj\ 再构建。MSBuild 的 /t:Rebuild 只保证重新编译，
# 不会清理 obj\ 里 XAML 编译等产生的中间缓存（wpftmp 项目、_MarkupCompile.cache 等），
# 遇到"改了 XAML/资源却像是没生效"这类诡异现象时用 -Clean 最可靠。
if ($Clean) {
    foreach ($relative in @('Mo3ModManager\bin', 'Mo3ModManager\obj')) {
        $path = Join-Path $root $relative
        if (Test-Path $path) {
            Remove-Item $path -Recurse -Force
            Write-Host "已清空缓存：$relative"
        }
    }
}

# 构建依赖（缺失时给出明确提示，而不是让 MSBuild 抛一堆 "未能找到类型 SharpCompress"）
$required = @(
    @{ Path = 'Newtonsoft.Json.11.0.2\lib\net40\Newtonsoft.Json.dll'; Package = 'Newtonsoft.Json 11.0.2' },
    @{ Path = 'SharpCompress.0.23.0\lib\net45\SharpCompress.dll';     Package = 'SharpCompress 0.23.0' },
    @{ Path = 'Microsoft.Net.Compilers.3.11.0\tools\csc.exe';         Package = 'Microsoft.Net.Compilers 3.11.0' },
    @{ Path = 'Microsoft.NETFramework.ReferenceAssemblies.net48.1.0.3\build\.NETFramework\v4.8\mscorlib.dll'; Package = 'Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3' }
)
$missing = @($required | Where-Object { -not (Test-Path (Join-Path $packagesDir $_.Path)) })
if ($missing.Count -gt 0) {
    $names = ($missing | ForEach-Object { $_.Package }) -join '、'
    throw "缺少构建依赖：$names。请先运行：powershell -ExecutionPolicy Bypass -File tools\setup-build-env.ps1"
}

# 选择 MSBuild：优先 32 位版本。
# 原因：MSBuild 4.0 的 GenerateResource 任务（处理 .resx）默认要求 CLR4/x86 的
# 任务宿主；用 32 位 MSBuild 时当前进程本身即为 CLR4/x86，任务可以就地运行，
# 不必创建进程外任务宿主（在受限环境下创建任务宿主会失败并报 MSB4216）。
$msbuildCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe')
)
$msbuild = @($msbuildCandidates | Where-Object { Test-Path $_ })[0]
if (-not $msbuild) {
    throw '找不到 MSBuild.exe（需要系统已安装 .NET Framework 4.x）。'
}

$cscToolPath = (Join-Path $packagesDir 'Microsoft.Net.Compilers.3.11.0\tools') + '\'

# .NET Framework 4.8 引用程序集（免安装版 Targeting Pack）。必须显式指向它：
# 否则 MSBuild 会因为找不到 v4.8 的引用程序集（本机没装 VS/SDK）而回退到
# 运行时目录里的程序集，除了报 MSB3644 警告，还会把 mscorlib.dll、norm*.nlp、
# zh-Hans 等文件一起复制进 bin\<Config>\ 输出目录（产物被污染）。
# 注意属性值必须以反斜杠结尾。
$targetFrameworkRootPath = (Join-Path $packagesDir 'Microsoft.NETFramework.ReferenceAssemblies.net48.1.0.3\build') + '\'

Write-Host "MSBuild      : $msbuild"
Write-Host "Roslyn csc   : $cscToolPath"
Write-Host "net48 引用程序集 : $targetFrameworkRootPath"
Write-Host "配置         : $Configuration / $Platform"
Write-Host ''

# 构建输出里会有两条无害的噪音警告，不必理会：
#   - "项目文件包含 ToolsVersion="15.0" ... 将该项目视为其已具有 ToolsVersion="4.0""：
#     项目文件是按 VS2022 写的，这里用的是系统自带的 4.0 版 MSBuild，回退是预期行为。
#   - MSB3270「架构 MSIL 与引用 mscorlib 的 x86 不匹配」：4.0 版 MSBuild 解析
#     mscorlib 时仍会带上自己位数的运行时目录，产物本身是 AnyCPU，不受影响。
& $msbuild $solution /t:Rebuild "/p:Configuration=$Configuration" "/p:Platform=$Platform" `
    "/p:CscToolPath=$cscToolPath" /p:CscToolExe=csc.exe `
    "/p:TargetFrameworkRootPath=$targetFrameworkRootPath" /p:DisableOutOfProcTaskHost=true /v:m /nologo

if ($LASTEXITCODE -ne 0) {
    throw "构建失败（MSBuild 退出码 $LASTEXITCODE）。"
}

$output = Join-Path $root "Mo3ModManager\bin\$Configuration\Mo3ModManager.exe"
Write-Host ''
Write-Host "构建成功：$output"
