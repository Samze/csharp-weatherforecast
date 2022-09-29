using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;

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
    public WeatherForecast Get()
    {
        //Parse RMQ configuration
        string host = Environment.GetEnvironmentVariable("RMQ_HOST");
        string port = Environment.GetEnvironmentVariable("RMQ_PORT");
        string user = Environment.GetEnvironmentVariable("RMQ_USER");
        string password = Environment.GetEnvironmentVariable("RMQ_PASSWORD");

        //Setup RMQ client
        ConnectionFactory factory = new ConnectionFactory();
        factory.UserName = user;
        factory.Password = password;
        factory.HostName = host;
        factory.Port = Int32.Parse(port);

        IConnection conn = factory.CreateConnection();
        
        IModel channel = conn.CreateModel();
        channel.QueueDeclare(queue: "temperature",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

        //Generate Weather Data
        var weather = new WeatherForecast
            {
                Date = DateTime.Now.AddDays(0),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            };

        //Send weather data
        var jsonWeather = JsonConvert.SerializeObject(weather);
    
        var body = Encoding.UTF8.GetBytes(jsonWeather);

        channel.BasicPublish(exchange: "",
                            routingKey: "temperature",
                            basicProperties: null,
                            body: body);

        Console.WriteLine(" [->] Sent {0}",jsonWeather);


        //Receive weather data
        var response = channel.BasicGet(queue: "temperature", autoAck: true);

        var recbody = response.Body.ToArray();
        var recmessage = Encoding.UTF8.GetString(recbody);
        Console.WriteLine(" [<-] Received {0}", recmessage);

        var recweather = JsonConvert.DeserializeObject < WeatherForecast > (recmessage);

        return recweather;
    }
}
