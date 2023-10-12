echo ++++ Starting Processes for Siemens Adapter Testing: RabbitMQ, CC-Disco, CC-Adapters-Siemens ++++
cd c:\ABS\cc-disco\
echo == Starting RabbitMQ ==
start "RabbitMQ" docker-run_rabbit-mq.bat
timeout 20 1>NUL
echo == Starting CC-Disco ==
start "CC-Disco" docker-run_cc-disco.bat
timeout 30 1>NUL
echo == Copying CC-Disco Config files ==
copy_cc-disco-configs.bat test-client
timeout 20 1>NUL
echo == Starting CC-Adapters-Siemens ==
start "CC-Adapters-Siemens" docker-run_siemens-adapter.bat