Write-Output "Importing certificate..."
& certoc.exe -addstore root C:\\certs\\rabbitmq.cer;

try{
  Write-Output "Starting RabbitMQ server...";
  Set-Location C:/;
  & ./rabbitmq/sbin/rabbitmq-server.bat;
  # Invoke-Expression -Command "./rabbitmq/sbin/rabbitmq-server.bat";
  # Get-Content -Path "C:\\rabbitmq\\sbin\\rabbitmq-server.bat";
  # cmd.exe -/c "C:\\rabbitmq\\sbin\\rabbitmq-server.bat";
  Write-Output "RabbitMQ stopped???";
  exit 0;
}
catch {
  $msg = @();
  $err = $_.Exception;
  do {
      $msg += $err.Message;
      $err = $err.InnerException;
  } while ($err)

  Write-Output "Fatal Exception: $($msg -join ' - ')";
  exit 1;
}