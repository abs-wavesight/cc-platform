$ScriptDirectory = $(Split-Path $MyInvocation.MyCommand.Path)
$RepoDirectory = (Get-Item $ScriptDirectory).Parent.Parent.Parent.FullName
$LocalConfigDirectory = "${RepoDirectory}/config/vector/config"

$Image = "ghcr.io/abs-wavesight/vector:windows-2019"

$ContainerConfigFilePath = "C:\\config\\vector.toml"

$TestFileDirectory = "${RepoDirectory}/config/vector/tests"
$TestFiles = Get-ChildItem $TestFileDirectory -Filter *.toml | ForEach-Object {"C:\\tests\\" + $_.Name}
$TestFilesParameter = $TestFiles -join " "

$Command = "docker run --mount type=bind,src=${LocalConfigDirectory},dst=C:/config --mount type=bind,src=${TestFileDirectory},dst=C:/tests ${Image} test ${ContainerConfigFilePath} ${TestFilesParameter}"
Write-Output "Docker Command: ${Command}"
Invoke-Expression $Command