param(
    [string]$CoberturaXmlPath,
    [string]$RootDirectory
)

function Get-ParentPrependedPath {
    Param(
        [Parameter(Mandatory = $true)]
        [String]$rootDir,
        
        [Parameter(Mandatory = $true)]
        [String]$partialPath
    )

    $rootDir = $rootDir.Replace("\", "/")
    $partialPath = $partialPath.Replace("\", "/")
    $splitPath = $partialPath.Split("/")
    $searchPhrase = $splitPath[-1]

    $directories = Get-ChildItem -Path $rootDir -Recurse -Force -File | Where-Object Name -eq $searchPhrase

    foreach($directory in $directories){
        # Prepends the immediate parent directory to the partial path
        $result = (($directory.Directory.FullName -replace [regex]::Escape($rootDir), '') + '/' + $splitPath[-1]).TrimStart('/')
        $result = $result.Replace("\", "/")
        $result = $result.Replace($rootDir, '').TrimStart('\').TrimStart('/')
            
        if($result.contains($partialPath)){
            return $result
        }
    }
}

# Load the XML document
$xml = [xml](Get-Content "$CoberturaXmlPath")

# For each class node
foreach ($classNode in $xml.SelectNodes("//class")) {
    # Get the filename attribute
    $filenameAttribute = $classNode.Attributes.GetNamedItem("filename")

    # Get the filename from filename attribute (last part of the path)
    $partialPath = $filenameAttribute.Value

    $prependedPath = Get-ParentPrependedPath -rootDir $RootDirectory -partialPath $partialPath

    if (![string]::IsNullOrEmpty($prependedPath)) {
        Write-Host "Replaced: $partialPath -> $prependedPath"
        $filenameAttribute.Value = $prependedPath
    }
    else {
        Write-Host "Skipped: $partialPath"
    }
}

# Save the modified XML to disk
$xml.OuterXml | Set-Content -Path "$CoberturaXmlPath"