# This file is built to mimic Sysinternals "handle" utility, which is used by RabbitMQ here: https://github.com/rabbitmq/rabbitmq-server/blob/085cc457450f53a3eada45e13cf0fc46ac3bff26/deps/rabbitmq_management_agent/src/rabbit_mgmt_external_stats.erl#L106

param(
  [Parameter(Position = 0, HelpMessage = 'Process ID')]
  [Alias('p')]
  [int]
  $InputPid,

  [Parameter(Position = 1, HelpMessage = 'Thrown away')]
  [Alias('s')]
  [Switch]
  $PrintCount,

  [Parameter(Position = 2, HelpMessage = 'Thrown away')]
  [string]
  $AcceptEula = ""
)

$TotalHandleCount = (Get-Process -Id $InputPid | Select-Object -ExpandProperty HandleCount)

Write-Output "
Nthandle v5.0 - Handle viewer
Copyright (C) 1997-2022 Mark Russinovich
Sysinternals - www.sysinternals.com

Handle type summary:
  <Unknown type>  : 0
  ALPC Port       : 0
  Desktop         : 0
  Directory       : 0
  Event           : 0
  File            : ${TotalHandleCount}
  IoCompletion    : 0
  IRTimer         : 0
  Key             : 0
  Mutant          : 0
  Section         : 0
  Semaphore       : 0
  Thread          : 0
  TpWorkerFactory : 0
  WaitCompletionPacket: 0
  WindowStation   : 0
Total handles: ${TotalHandleCount}"