namespace ServiceWorker;
using System.IO;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class WorkerPlanning : BackgroundService
{
    private readonly ILogger<WorkerPlanning> _logger;
    private readonly string _filePath;
    private readonly string _hostName;

    public WorkerPlanning(ILogger<WorkerPlanning> logger, IConfiguration config)
    {
        _logger = logger;
        _filePath = config["FilePath"] ?? "/srv";
        _hostName = config["HostnameRabbit"];

        _logger.LogInformation($"Filepath: {_filePath}");
        _logger.LogInformation($"Connection: {_hostName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a new instance of the ConnectionFactory
        var factory = new ConnectionFactory { HostName = _hostName };

        // Create a new connection to rabbitMQ using the ConnectionFactory
        using var connection = factory.CreateConnection();
        // Create a new channel using the connection
        using var channel = connection.CreateModel();

        // Declare a topic exchange named "topic_fleet"
        channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

        var queueName = channel.QueueDeclare().QueueName;

        channel.QueueBind(queue: queueName, exchange: "topic_fleet", routingKey: "PlanDTO");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Deserialiserer det indsendte data om til C# objekt
            PlanDTO? plan = JsonSerializer.Deserialize<PlanDTO>(message);

            if (
                !File.Exists(Path.Combine(_filePath, "plan.csv"))
                || new FileInfo(Path.Combine(_filePath, "plan.csv")).Length == 0
            )
            {
                using (
                    StreamWriter outputFile = new StreamWriter(Path.Combine(_filePath, "plan.csv"))
                )
                {
                    outputFile.WriteLine("Kundenavn,Starttidspunkt,Startsted,Slutsted");
                    outputFile.Close();
                }
            }

            // StreamWriter til at sende skrive i .CSV-filen
            using (
                StreamWriter outputFile = new StreamWriter(
                    Path.Combine(_filePath, "plan.csv"),
                    true
                )
            )
            {
                _logger.LogInformation("Ny booking skrevet i plan.csv");
                // Laver en ny linje med det tilsendte data og lukker filen.
                outputFile.WriteLineAsync(
                    $"{plan.KundeNavn},{plan.StartTidspunkt},{plan.StartSted},{plan.SlutSted}"
                );
                outputFile.Close();
            }
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
