using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client;

[ApiController]
[Route("[controller]")]
public class TaxabookingController : ControllerBase
{
    private readonly ILogger<TaxabookingController> _logger;
    private readonly string _filePath;
    private readonly string _hostName;

    public TaxabookingController(ILogger<TaxabookingController> logger, IConfiguration config)
    {
        _logger = logger;
        _filePath = config["FilePath"] ?? "/srv";
        _hostName = config["HostnameRabbit"];

        _logger.LogInformation($"Filepath: {_filePath}");
        _logger.LogInformation($"Connection: {_hostName}");

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Taxabooking responding from {_ipaddr}");
    }

    // Opretter Plan ud fra Booking
    [HttpPost("opret")]
    public IActionResult OpretBooking(BookingDTO booking)
    {
        PlanDTO plan = new PlanDTO(
            booking.KundeNavn,
            booking.StartTidspunkt,
            booking.StartSted,
            booking.SlutSted
        );

        try
        {
            // Opretter forbindelse til RabbitMQ
            var factory = new ConnectionFactory { HostName = _hostName };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

            // Serialiseres til JSON
            string message = JsonSerializer.Serialize(plan);

            // Konverteres til byte-array
            var body = Encoding.UTF8.GetBytes(message);

            // Sendes til kø
            channel.BasicPublish(
                exchange: "topic_fleet",
                routingKey: "PlanDTO",
                basicProperties: null,
                body: body
            );

            _logger.LogInformation("Plan oprettet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }
        return Ok(plan);
    }

    // Henter CSV-fil
    [HttpGet("hent")]
    public async Task<ActionResult> HentPlan()
    {
        try
        {
            //Læser indholdet af CSV-fil fra filsti (_filePath)
            var bytes = await System.IO.File.ReadAllBytesAsync(Path.Combine(_filePath, "plan.csv"));

            _logger.LogInformation("plan.csv modtaget");

            // Returnere CSV-filen med indholdet
            return File(bytes, "text/csv", Path.GetFileName(Path.Combine(_filePath, "plan.csv")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }
    }
}
