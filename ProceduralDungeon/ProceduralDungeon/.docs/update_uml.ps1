param (
    [string]$ScriptsPath = "..\Assets\Scripts",
    [string]$OutputName = "full_project.puml",
    [switch]$HidePrivate
)

Write-Host "--- Starting PlantUML Generation ---" -ForegroundColor Cyan

# =========================================================
# BUFFER
# =========================================================

$builder = [System.Text.StringBuilder]::new()

function Write-UML {
    param([string]$Text)
    [void]$builder.AppendLine($Text)
}

# =========================================================
# HEADER
# =========================================================

Write-UML @"
@startuml

set namespaceSeparator none
left to right direction

skinparam BackgroundColor #FDFDFD
skinparam RoundCorner 8

skinparam class {
    BackgroundColor #EDF2F7
    BorderColor #A0AEC0
    FontColor #2D3748

    BackgroundColor<<MB>> #D1D9E6
    BorderColor<<MB>> #8A9AAB

    BackgroundColor<<SO>> #D2E4D8
    BorderColor<<SO>> #94B49F
}

hide empty members

!define MonoBehaviour(x) class x << (M,#8A9AAB) MB >>
!define ScriptableObject(x) class x << (S,#94B49F) SO >>
!define PrivateSerialized(x) - [<color:#00C732>S</color>] x
!define PublicSerialized(x) + [<color:#00C732>S</color>] x

"@

# =========================================================
# GLOBALS
# =========================================================

$relationships = [System.Collections.Generic.HashSet[string]]::new()
$knownTypes = [System.Collections.Generic.HashSet[string]]::new()
$typeInfo = @{}

# =========================================================
# HELPERS
# =========================================================

function Clean-TypeName {
    param([string]$Type)
    if ([string]::IsNullOrWhiteSpace($Type)) { return "" }
    return ($Type -replace '\s+', ' ' -replace '\bwhere\b.*', '').Trim()
}

function Get-RawTypeName {
    param([string]$Type)
    if ($Type -match '([A-Za-z_]\w*)') { return $Matches[1] }
    return $Type
}

function Remove-Comments {
    param([string]$Content)
    $content = [regex]::Replace($Content, '/\*.*?\*/', '', 'Singleline')
    $content = [regex]::Replace($content, '//.*', '')
    return $content
}

function Get-ClassBody {
    param([string]$Content, [int]$OpenBraceIndex)
    $depth = 1
    $i = $OpenBraceIndex + 1
    while ($i -lt $Content.Length -and $depth -gt 0) {
        switch ($Content[$i]) {
            '{' { $depth++ }
            '}' { $depth-- }
        }
        $i++
    }
    return $Content.Substring($OpenBraceIndex + 1, ($i - $OpenBraceIndex - 2))
}

function Get-TopLevelTypes {
    param([string]$Content)
    $results = @()
    # Updated to better handle generic brackets and optional modifiers
    $regex = [regex]::new('(?ms)(?<mods>(?:public|private|protected|internal|abstract|sealed|static|partial)\s+)*(?<kind>class|interface|struct)\s+(?<name>[A-Za-z_]\w*(?:\s*<[^>]+>)?)\s*(?:\:\s*(?<parents>[^{]+))?\s*\{')
    foreach ($m in $regex.Matches($Content)) {
        $results += [PSCustomObject]@{
            Kind = $m.Groups["kind"].Value
            Name = Clean-TypeName $m.Groups["name"].Value
            Parents = Clean-TypeName $m.Groups["parents"].Value
            OpenBraceIndex = $m.Index + $m.Length - 1
        }
    }
    return $results
}

function Add-Relationship {
    param([string]$From, [string]$Arrow, [string]$To)
    if ([string]::IsNullOrWhiteSpace($From) -or [string]::IsNullOrWhiteSpace($To) -or $From -eq $To) { return }
    [void]$relationships.Add("$From $Arrow $To")
}

# =========================================================
# DISCOVER TYPES
# =========================================================

$scripts = Get-ChildItem $ScriptsPath -Recurse -Filter *.cs -File

foreach ($file in $scripts) {
    $content = Remove-Comments(Get-Content $file.FullName -Raw)
    foreach ($type in (Get-TopLevelTypes $content)) {
        $raw = Get-RawTypeName $type.Name
        [void]$knownTypes.Add($raw)
        $typeInfo[$raw] = $type
    }
}

# =========================================================
# PARSE FILES
# =========================================================

foreach ($file in $scripts) {
    Write-Host " -> $($file.Name)" -ForegroundColor DarkGray
    $content = Remove-Comments(Get-Content $file.FullName -Raw)

    foreach ($type in (Get-TopLevelTypes $content)) {
        $kind = $type.Kind
        $typeName = $type.Name
        $parents = $type.Parents
        $rawType = Get-RawTypeName $typeName

        Write-Host "    [+] $kind $typeName" -ForegroundColor Green

        if ($parents -match '\bMonoBehaviour\b') { Write-UML "MonoBehaviour($typeName) {" }
        elseif ($parents -match '\bScriptableObject\b') { Write-UML "ScriptableObject($typeName) {" }
        else { Write-UML "$kind $typeName {" }

        $body = Get-ClassBody $content $type.OpenBraceIndex
        $members = New-Object System.Collections.Generic.List[string]

        # --- FIELD REGEX (Handles Multi-line [SerializeField]) ---
        # Added '?' after the mods group to make it optional
$fieldPattern = '(?m)(?<serialized>\[SerializeField\]\s*)?(?<mods>(?:public|private|protected|internal|static|readonly)\s+)?(?<type>[A-Za-z_][\w<>\[\]\?\,\.\s]*)\s+(?<name>[A-Za-z_]\w*)\s*(?:=|;)'
        
        foreach ($match in [regex]::Matches($body, $fieldPattern)) {
            $isSerialized = !([string]::IsNullOrWhiteSpace($match.Groups["serialized"].Value))
            $visibilityMods = $match.Groups["mods"].Value
            $visibility = if ($visibilityMods -match 'public') { "+" } else { "-" }
            $fType = Clean-TypeName $match.Groups["type"].Value
            $fName = $match.Groups["name"].Value

            if (-not $HidePrivate -or $visibility -eq "+") {
                if ($isSerialized) {
                    $macro = if ($visibility -eq "+") { "PublicSerialized" } else { "PrivateSerialized" }
                    $members.Add("$macro($fName : $fType)")
                } else {
                    $members.Add("$visibility $fName : $fType")
                }
            }

            foreach ($known in $knownTypes) {
    # Check if the field type contains the known type name (e.g., List<CandyInstance> contains CandyInstance)
    if ($fType -match "\b$known\b" -and $known -ne $rawType) {
        Add-Relationship $rawType "-->" $known
    }
}
        }

        # --- METHOD REGEX (Filters local variables and flow control) ---
        $methodPattern = '(?m)(?<mods>(?:public|private|protected|internal|static|virtual|override|async)\s+)?(?<ret>[A-Za-z_][\w<>\[\]\?\,\.\s]*)\s+(?<name>[A-Za-z_]\w*)\s*\('
        
        foreach ($match in [regex]::Matches($body, $methodPattern)) {
            $mName = $match.Groups["name"].Value
            $mRet = Clean-TypeName $match.Groups["ret"].Value
            if ($mName -match '^(if|for|foreach|while|switch|using|new)$') { continue }

            $visibility = if ($match.Groups["mods"].Value -match 'public') { "+" } else { "-" }
            if (-not $HidePrivate -or $visibility -eq "+") {
                $members.Add("$visibility $mName() : $mRet")
            }
        }

        # Unique Output
        $members | Select-Object -Unique | ForEach-Object { Write-UML "  $_" }
        Write-UML "}"

        # Inheritance
        if ($parents) {
            foreach ($parent in ($parents -split ',')) {
                $parentName = Get-RawTypeName $parent.Trim()
                if ($knownTypes.Contains($parentName) -and $parentName -notmatch 'MonoBehaviour|ScriptableObject') {
                    $parentInfo = $typeInfo[$parentName]
                    $arrow = if ($parentInfo.Kind -eq "interface") { "<|.." } else { "<|--" }
                    Add-Relationship $parentName $arrow $rawType
                }
            }
        }
    }
}

# =========================================================
# RELATIONSHIPS
# =========================================================

Write-UML ""
$relationships | Sort-Object | ForEach-Object { Write-UML $_ }
Write-UML "@enduml"

# =========================================================
# WRITE FILE
# =========================================================

$AbsoluteOutput = Join-Path (Get-Location) $OutputName
[System.IO.File]::WriteAllText($AbsoluteOutput, $builder.ToString(), [System.Text.Encoding]::UTF8)

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
Write-Host "Generated: $OutputName" -ForegroundColor Yellow