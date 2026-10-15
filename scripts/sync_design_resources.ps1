[CmdletBinding()]
param([switch]$Check)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$translations = [ordered]@{}
foreach ($culture in @('zh-CN', 'zh-Hant', 'en-US', 'ja-JP')) {
    $resource = Get-Content (Join-Path $root "src/Zhijian/I18n/$culture.json") -Raw | ConvertFrom-Json -AsHashtable
    $translations[$culture] = $resource.Zhijian.ZhijianL
}
$palettes = Get-Content (Join-Path $root 'scripts/prototype/assets/palettes.json') -Raw | ConvertFrom-Json -AsHashtable
$content = '// 由 scripts/sync_design_resources.ps1 从宿主 JSON 和固定主题数据生成。' + "`nwindow.translations = " + (ConvertTo-Json $translations -Depth 10) + ";`nwindow.palettes = " + (ConvertTo-Json $palettes -Depth 10) + ";`n"
$path = Join-Path $root 'scripts/prototype/assets/resources.js'
if ($Check) {
    if ((Get-Content $path -Raw).Replace("`r`n", "`n") -ne $content.Replace("`r`n", "`n")) { throw 'Prototype resources are out of date.' }
} else { [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false)) }

& (Join-Path $PSScriptRoot 'build_standalone_prototype.ps1') -Check:$Check
