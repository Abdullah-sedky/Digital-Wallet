using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Microsoft.Identity.Client;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

public class RabbitMqPublisher
{
    private readonly IConnection _connection;
    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;
    }
   
    public virtual async Task PublishTransactionEvent(object message)
    {
        if (_connection == null) return;
        using var channel = await _connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: "transactions", durable: true, exclusive: false, autoDelete: false);
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        await channel.BasicPublishAsync(exchange: "", routingKey: "transactions", body: body);
    }
}
