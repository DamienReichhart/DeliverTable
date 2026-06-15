using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace DeliverTableInfrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private const string ExchangeName = "delivertable.jobs";
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(RabbitMqConfig config)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Port,
            UserName = config.User,
            Password = config.Password
        };

        IConnection connection = await factory.CreateConnectionAsync();
        IChannel channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true);

        return new RabbitMqPublisher(connection, channel);
    }

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct = default) where T : class
    {
        string json = JsonSerializer.Serialize(message);
        byte[] body = Encoding.UTF8.GetBytes(json);

        BasicProperties properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        _channel.Dispose();
        _connection.Dispose();
    }
}
