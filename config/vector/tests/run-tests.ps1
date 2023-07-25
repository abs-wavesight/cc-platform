$ScriptDirectory = $(Split-Path $MyInvocation.MyCommand.Path)
$RepoDirectory = (Get-Item $ScriptDirectory).Parent.Parent.Parent.FullName

$Image = "vector-local-for-tests:windows-2019"

# First, build a local docker image so we're testing the latest version of the image, rather than the one from the registry
$BuildCommand = "docker build -f $RepoDirectory/containers/vector/Dockerfile -t $Image --build-arg SERVER_VERSION=ltsc2019 --build-arg DOTNET_TAG=7.0-nanoserver-1809 --build-arg POWERSHELL_NANOSERVER_TAG=lts-7.2-nanoserver-1809 --platform windows/amd64 containers/vector"
Write-Output "Docker Build Command: ${BuildCommand}"
Invoke-Expression $BuildCommand

$VectorVariants = @("site", "central")
foreach($VectorVariant in $VectorVariants)
{
  $LocalConfigDirectory = "${RepoDirectory}/config/vector/config/$VectorVariant"
  $ContainerConfigFilePath = "C:/config/vector.toml"
  $TestFileDirectory = "${RepoDirectory}/config/vector/tests/$VectorVariant"
  $TestFiles = Get-ChildItem $TestFileDirectory -Filter *.toml | ForEach-Object {"C:/tests/" + $_.Name}
  $ConfigFilesParameter = $TestFiles -join ","
  $ConfigFilesParameter = $ConfigFilesParameter += ",$ContainerConfigFilePath"

  $RunCommand = "docker run --mount type=bind,src=${LocalConfigDirectory},dst=C:/config --mount type=bind,src=${TestFileDirectory},dst=C:/tests -e VECTOR_LOG_FORMAT=json -e VECTOR_COLOR=never -e VECTOR_CONFIG=$ConfigFilesParameter -e VECTOR_TEST=true ${Image} test"
  Write-Output "Docker Run Command: ${RunCommand}"
  Invoke-Expression ". { $RunCommand } 2>&1"
}