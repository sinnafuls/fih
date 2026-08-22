param([string[]]$Types, [switch]$FieldsOnly, [string]$MethodFilter = '')

$game = 'D:\SteamLibrary\steamapps\common\How to Fish\How to Fish'
Add-Type -Path "$game\BepInEx\core\Mono.Cecil.dll"
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$game\How to Fish_Data\Managed\Assembly-CSharp.dll")

function Sig($m) {
    $ps = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
    $mods = @()
    if ($m.IsStatic) { $mods += 'static' }
    if ($m.IsPrivate) { $mods += 'private' } elseif ($m.IsPublic) { $mods += 'public' }
    '    {0,-24} {1} {2}({3})' -f ($mods -join ' '), $m.ReturnType.Name, $m.Name, $ps
}

foreach ($tn in $Types) {
    $found = @($asm.MainModule.Types | Where-Object { $_.Name -eq $tn -or $_.FullName -eq $tn })
    $t = @($found | Where-Object { $_.Namespace -eq '' })[0]
    if (-not $t) { $t = $found[0] }
    if (-not $t) { "TYPE NOT FOUND: $tn"; continue }
    "=== $($t.FullName) : $($t.BaseType.Name) ==="
    '--- fields ---'
    $t.Fields | ForEach-Object {
        $mods = @()
        if ($_.IsStatic) { $mods += 'static' }
        if ($_.IsPublic) { $mods += 'public' } else { $mods += 'private' }
        '    {0,-16} {1,-28} {2}' -f ($mods -join ' '), $_.FieldType.Name, $_.Name
    }
    if (-not $FieldsOnly) {
        '--- methods ---'
        $ms = $t.Methods | Where-Object { -not $_.IsGetter -and -not $_.IsSetter }
        if ($MethodFilter) { $ms = $ms | Where-Object { $_.Name -match $MethodFilter } }
        $ms | ForEach-Object { Sig $_ }
    }
    ''
}
