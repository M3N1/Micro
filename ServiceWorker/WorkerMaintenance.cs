namespace ServiceWorker;
using System.IO;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class WorkerMaintenance : BackgroundService
{
    private readonly ILogger<WorkerMaintenance> _logger;
    private readonly string _filePath;
    private readonly string _hostName;

    public WorkerMaintenance(ILogger<WorkerMaintenance> logger, IConfiguration config)
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

        channel.QueueBind(queue: queueName, exchange: "topic_fleet", routingKey: "maintenance.*");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Deserialiserer det indsendte data om til C# objekt
            MaintenanceDTO? maintenance = JsonSerializer.Deserialize<MaintenanceDTO>(message);

            if (
                !File.Exists(Path.Combine(_filePath, maintenance.OpgType + ".csv"))
                || new FileInfo(Path.Combine(_filePath, maintenance.OpgType + ".csv")).Length == 0
            )
            {
                using (
                    StreamWriter outputFile = new StreamWriter(
                        Path.Combine(_filePath, maintenance.OpgType + ".csv")
                    )
                )
                {
                    outputFile.WriteLine("Id,Beskrivelse,OpgType,Ansvarlig");
                    outputFile.Close();
                }
            }

            // StreamWriter til at sende skrive i .CSV-filen
            using (
                StreamWriter outputFile = new StreamWriter(
                    Path.Combine(_filePath, maintenance.OpgType + ".csv"),
                    true
                )
            )
            {
                _logger.LogInformation("Ny maintenance skrevet i " + maintenance.OpgType + ".csv");
                // Laver en ny linje med det tilsendte data og lukker filen.
                outputFile.WriteLineAsync(
                    $"{maintenance.Id},{maintenance.Beskrivelse},{maintenance.OpgType},{maintenance.Ansvarlig}"
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
