using Microsoft.AspNetCore.Mvc;

namespace Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<String> Get()
    {

      // Check whether the environment variable exists.
        string host = Environment.GetEnvironmentVariable("RMQ_HOST");
        string port = Environment.GetEnvironmentVariable("RMQ_PORT");
        string user = Environment.GetEnvironmentVariable("RMQ_USER");
        string password = Environment.GetEnvironmentVariable("RMQ_PASSWORD");
        return new string[]{host, port, user, password};
        // return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //     {
        //         Date = DateTime.Now.AddDays(index),
        //         TemperatureC = Random.Shared.Next(-20, 55),
        //         Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //     })
        //     .ToArray();
    }
}
