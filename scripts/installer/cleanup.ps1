param(
    [string]$FilePath = "$(Split-Path -Parent $MyInvocation.MyCommand.Path)\image_list.txt",
    [string]$DockerPath = "docker"
)

function Write-Log {
    param([string]$message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $message"
}

# Check if the docker path is valid
if (-not (Get-Command $DockerPath -ErrorAction SilentlyContinue)) {
    Write-Log "❌ Docker executable not found at path '$DockerPath'"
    exit 1
}

# Log all current images and containers before cleanup
$allImagesBefore = & $DockerPath images --format "{{.Repository}}:{{.Tag}}"
$allContainersBefore = & $DockerPath ps -a --format "{{.Image}} [{{.ID}}]"

Write-Log "========== Docker Cleanup Started =========="
Write-Log "👀 List of all images:"
$allImagesBefore | ForEach-Object { Write-Log "  $_" }

Write-Log "👀 List of all containers:"
$allContainersBefore | ForEach-Object { Write-Log "  $_" }

if (-Not (Test-Path $FilePath)) {
    Write-Log "❌ Image list file not found: $FilePath"
    exit 1
}

$imageTags = Get-Content $FilePath | Where-Object { $_ -and ($_ -notmatch '^\s*#') }

foreach ($line in $imageTags) {
    Write-Host ""
    if ($line -match "^([^:]+):(.+)$") {
        $imageName = $Matches[1]
        $expectedTag = $Matches[2]
        $targetImage = "${imageName}:${expectedTag}"
        Write-Log "🔍 Checking image: $targetImage"

        $existingImages = & $DockerPath images --format "{{.Repository}}:{{.Tag}}" | Where-Object { $_ -like "${imageName}:*" }

        if ($existingImages) {
            Write-Log "--> ✅ Detected images: $($existingImages -join ', ')"
        } else {
            Write-Log "--> ❎ No images found for $imageName"
            continue
        }

        foreach ($img in $existingImages) {
            if ($img -eq $targetImage) {
                Write-Log "--> ⏭ Skipping target image: $img"
                continue
            }

            Write-Log "--> ❌ Removing image: $img"

            $containers = & $DockerPath ps -a --filter "ancestor=$img" --format "{{.ID}}"
            foreach ($containerId in $containers) {
                $containerInfo = & $DockerPath ps -a --filter "id=$containerId" --format "{{.Image}} [{{.ID}}]"
                Write-Log "----> ❌ Removing container: $containerInfo"
                & $DockerPath stop $containerId | Out-Null
                & $DockerPath rm $containerId | Out-Null
                Write-Log "----> ✅ Removed container: $containerInfo"
            }

            & $DockerPath rmi -f $img | Out-Null
            Write-Log "--> ✅ Removed image: $img"
        }

        & $DockerPath volume prune -f | Out-Null
        & $DockerPath system prune -a -f --volumes | Out-Null
        Write-Log "-> 🧹 Pruned unused volumes and system resources."
    } else {
        Write-Log "⚠️ Invalid image format: $line. Use image:tag."
    }
}

Write-Host ""
$allImagesAfter = & $DockerPath images --format "{{.Repository}}:{{.Tag}}"
$allContainersAfter = & $DockerPath ps -a --format "{{.Image}} [{{.ID}}]"

Write-Log "📦 List of all images after cleanup:"
$allImagesAfter | ForEach-Object { Write-Log "  $_" }

Write-Log "🚢 List of all containers after cleanup:"
$allContainersAfter | ForEach-Object { Write-Log "  $_" }

Write-Log "========== ✅ Docker Cleanup Complete =========="


