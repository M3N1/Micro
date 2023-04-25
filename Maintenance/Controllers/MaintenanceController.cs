using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client;

[ApiController]
[Route("[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly ILogger<MaintenanceController> _logger;
    private readonly string _filePath;
    private readonly string _hostName;

    public MaintenanceController(ILogger<MaintenanceController> logger, IConfiguration config)
    {
        _logger = logger;
        _filePath = config["FilePath"] ?? "/srv";
        _hostName = config["HostnameRabbit"];

        _logger.LogInformation($"Filepath: {_filePath}");
        _logger.LogInformation($"Connection: {_hostName}");

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"Maintenance responding from {_ipaddr}");
    }

    // Opretter Plan ud fra Booking
    [HttpPost("opret")]
    public IActionResult OpretMaintenance(ServiceDTO service)
    {
        if (service.OpgType == "Service" || service.OpgType == "Reparation")
        {
            MaintenanceDTO maintenance = new MaintenanceDTO(
                service.Id,
                service.Beskrivelse,
                service.OpgType,
                service.Ansvarlig
            );
            try
            {
                // Opretter forbindelse til RabbitMQ
                var factory = new ConnectionFactory { HostName = _hostName };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "topic_fleet", type: ExchangeType.Topic);

                // Serialiseres til JSON
                string message = JsonSerializer.Serialize(maintenance);

                // Konverteres til byte-array
                var body = Encoding.UTF8.GetBytes(message);

                // Sendes til kø
                channel.BasicPublish(
                    exchange: "topic_fleet",
                    routingKey: "maintenance." + service.OpgType,
                    basicProperties: null,
                    body: body
                );

                _logger.LogInformation("Maintenance oprettet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
            return Ok(maintenance);
        }
        else
        {
            _logger.LogError("Opgavetype er ikke Service eller Reparation");
            return StatusCode(500);
        }
    }

    // Henter CSV-fil
    [HttpGet("hent/service")]
    public async Task<ActionResult> HentService()
    {
        try
        {
            //Læser indholdet af CSV-fil fra filsti (_filePath)
            var bytes = await System.IO.File.ReadAllBytesAsync(
                Path.Combine(_filePath, "Service.csv")
            );

            _logger.LogInformation("Service.csv modtaget");

            // Returnere CSV-filen med indholdet
            return File(
                bytes,
                "text/csv",
                Path.GetFileName(Path.Combine(_filePath, "Service.csv"))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }
    }

    // Henter CSV-fil
    [HttpGet("hent/reparation")]
    public async Task<ActionResult> HentReparation()
    {
        try
        {
            //Læser indholdet af CSV-fil fra filsti (_filePath)
            var bytes = await System.IO.File.ReadAllBytesAsync(
                Path.Combine(_filePath, "Reparation.csv")
            );

            _logger.LogInformation("Reparation.csv modtaget");

            // Returnere CSV-filen med indholdet
            return File(
                bytes,
                "text/csv",
                Path.GetFileName(Path.Combine(_filePath, "Reparation.csv"))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500);
        }
    }
}
