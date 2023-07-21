# Set up log cleanup scheduled task
$LogRetentionPeriod = $env:LOG_RETENTION_PERIOD
if ([string]::IsNullOrWhitespace($LogRetentionPeriod)) 
{
  $LogRetentionPeriod = "90d"
}
Write-Output "ABS Wavesight Log Retention Period: ${LogRetentionPeriod}"
Write-Output "Running script to set up log retention scheduled task..."
& "C:\\scripts\\create-scheduled-task.ps1" -LogsDir "C:\\cc-logs" -Before $LogRetentionPeriod

# Run Vector
& "C:\\Program Files\\Vector\\bin\\vector.exe"