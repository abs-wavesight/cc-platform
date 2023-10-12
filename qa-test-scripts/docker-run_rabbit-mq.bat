echo == Starting RabbitMQ ==
cd c:\ABS\TestClient\
docker run -p 15672:15672 -p 5672:5672 -h rabbitmq-local ghcr.io/abs-wavesight/rabbitmq:windows-2019