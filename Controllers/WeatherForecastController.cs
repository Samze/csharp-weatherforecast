using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using KubeServiceBinding;
using StackExchange.Redis;

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
        // DotnetServiceBinding sc = new DotnetServiceBinding();
        // // Dictionary<string, string> rmqBinding = sc.GetBindings("rabbitmq");

        // Dictionary<string, string> redisBinding = sc.GetBindings("redis");

        // ConfigurationOptions config = new ConfigurationOptions
        // {
        //         EndPoints = { redisBinding["host"], redisBinding["port"] },
        //         Password = redisBinding["password"]
        // };

        //Setup RMQ client
        // ConnectionFactory factory = new ConnectionFactory();
        // factory.HostName = rmqBinding["host"];
        // factory.UserName = rmqBinding["username"];
        // factory.Password = rmqBinding["password"];
        // factory.Port = Int32.Parse(rmqBinding["port"]);

        // IConnection conn = factory.CreateConnection();
        
        IModel channel = RabbitmqClient();
        channel.QueueDeclare(queue: "temperature",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);


        var redisConn = RedisClient();
        IDatabase db = redisConn.GetDatabase();

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

        //Store data in Redis
        db.StringSet("temperature", recmessage);
        Console.WriteLine(" [!] Stored in Redis {0}", recmessage);
        return recweather;
    }


    private IModel RabbitmqClient() {
        DotnetServiceBinding sc = new DotnetServiceBinding();
        Dictionary<string, string> rmqBinding = sc.GetBindings("rabbitmq");

        //Setup RMQ client
        ConnectionFactory factory = new ConnectionFactory();
        factory.HostName = rmqBinding["host"];
        factory.UserName = rmqBinding["username"];
        factory.Password = rmqBinding["password"];
        factory.Port = Int32.Parse(rmqBinding["port"]);

        IConnection conn = factory.CreateConnection();
        
        return conn.CreateModel();
    }


    private ConnectionMultiplexer RedisClient() {
        DotnetServiceBinding sc = new DotnetServiceBinding();
        Dictionary<string, string> redisBinding = sc.GetBindings("redis");

        ConfigurationOptions config = new ConfigurationOptions
        {
                EndPoints = { redisBinding["host"], redisBinding["port"] },
                Password = redisBinding["password"]
        };

        return ConnectionMultiplexer.Connect(config.ToString());
    }
}
