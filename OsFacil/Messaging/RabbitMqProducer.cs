using System.Text;
using RabbitMQ.Client;

namespace OsFacil.Messaging;

public class RabbitMqProducer(IConfiguration config)
{
    public virtual void SendMessage(string message)
    {
        var factory = new ConnectionFactory { HostName = config["RabbitMQ:Host"] ?? "localhost" };

        
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "ordem de Serviço criada",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: "",
                             routingKey: "Ordem de Serviço criada",
                             basicProperties: null,
                             body: body);
    }
}