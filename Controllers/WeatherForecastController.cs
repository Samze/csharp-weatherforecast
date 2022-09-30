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

        //Setup redis client
        var redisConn = RedisClient();
        IDatabase db = redisConn.GetDatabase();

        //Setup rmq client
        IModel channel = RabbitmqClient();
        channel.QueueDeclare(queue: "weather",
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

        //Send weather data to RMQ
        var jsonWeather = JsonConvert.SerializeObject(weather);
    
        var body = Encoding.UTF8.GetBytes(jsonWeather);

        channel.BasicPublish(exchange: "",
                            routingKey: "weather",
                            basicProperties: null,
                            body: body);

        Console.WriteLine(" [->] Sent {0}",jsonWeather);


        //Receive weather data from RMQ
        var response = channel.BasicGet(queue: "weather", autoAck: true);

        var recbody = response.Body.ToArray();
        var recmessage = Encoding.UTF8.GetString(recbody);
        Console.WriteLine(" [<-] Received {0}", recmessage);


        //Store weather data in Redis
        db.StringSet("weather", recmessage);
        Console.WriteLine(" [!] Stored in Redis {0}", recmessage);

        //Return in JSON
        var recweather = JsonConvert.DeserializeObject < WeatherForecast > (recmessage);
        return recweather;
    }


    private IModel RabbitmqClient() {
        DotnetServiceBinding sc = new DotnetServiceBinding();
        Dictionary<string, string> rmqBinding = sc.GetBindings("rabbitmq");

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
            AbortOnConnectFail = true,
            Ssl = false, //WARNING UNSAFE
            ConnectRetry = 3,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            DefaultDatabase = 0,
            EndPoints = { { redisBinding["host"],  Int32.Parse(redisBinding["port"]) } },
            Password = redisBinding["password"]
        };

        Console.WriteLine("Redis creds {0}", config.ToString());

        return ConnectionMultiplexer.Connect(config);
    }
}
