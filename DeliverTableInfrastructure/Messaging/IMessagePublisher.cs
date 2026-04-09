namespace DeliverTableInfrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string routingKey, T message) where T : class;
}
