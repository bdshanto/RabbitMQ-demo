using System.Text;
using RabbitMQ.Client;

namespace Send;

internal class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory { HostName = "localhost", Password = "admin", UserName = "admin" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "hello",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var message = "This is the test message, that i am sending";
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: string.Empty,
            routingKey: "hello",
            basicProperties: null,
            body: body);
        Console.WriteLine($" [x] Sent {message}");

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}