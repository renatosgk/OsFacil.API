using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OsFacil.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IConfiguration _config;
        private IConnection _connection;
        private IModel _channel;
        private readonly string _queueName = "os-criada";

        public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            
            var factory = new ConnectionFactory { HostName = _config["RabbitMQ:Host"] ?? "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("[RabbitMQ] Aguardando novas Ordens de Serviço na fila '{Queue}'...", _queueName);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    
                    _logger.LogInformation("[Consumer] OS Recebida para Processamento: {Message}", message);

                   
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem do RabbitMQ.");
                    
                }
            };

            _channel.BasicConsume(queue: _queueName,
                                 autoAck: false,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
