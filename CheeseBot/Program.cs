using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using CheeseBot.Services;
using CheeseBot.Database.Collections;
using MongoDB.Driver;
using MongoDB.Bson;

namespace CheeseBot
{
    class Program
    {
        public static MongoClient client;
        public static IMongoDatabase database;

        public static RaffleService _raffleService;
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            try
            {
                if (Configuration.CreateSampleConfigFile())
                {
                    Console.WriteLine("Configuration File Created! Please close this application, modify the config file and re-run the application.");
                    Console.ReadLine();
                }
                else
                {
                    Configuration config = new Configuration();
                    config = config.LoadConfiguration();

                    if (string.IsNullOrEmpty(config.MongoHost) || string.IsNullOrEmpty(config.MongoPort))
                    {
                        Console.WriteLine("Database not configured! Check config.json.....");
                        Console.Read();
                    }
                    else
                    {
                        if(string.IsNullOrEmpty(config.MongoUsername) || string.IsNullOrEmpty(config.MongoPassword))
                        {
                            
                            client = new MongoClient($"mongodb://{config.MongoHost}:{config.MongoPort}");
                        }
                        else
                        {
                            client = new MongoClient($"mongodb://{config.MongoUsername}:{Uri.EscapeDataString(config.MongoPassword)}@{config.MongoHost}:{config.MongoPort}");
                        }
                        database = client.GetDatabase(config.MongoDatabase);
                        bool isMongoLive = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
                        if (!isMongoLive)
                        {
                            await CheeseLogAsync($"Failed to connect to database! {config.MongoDatabase}");
                        }
                        else
                        {
                            await CheeseLogAsync($"Connected to MongoDB on {config.MongoHost}:{config.MongoPort}");
                            await CheeseLogAsync("Cheese Bot Starting...");

                            //Initalize Collections
                            GuildCollection.collection = database.GetCollection<GuildCollection>("Guild");
                            DeveloperCollection.collection = database.GetCollection<DeveloperCollection>("Developer");

                            using (var services = ConfigureServices())
                            {
                                var discordclient = services.GetRequiredService<DiscordSocketClient>();

                                discordclient.Log += LogAsync;
                                services.GetRequiredService<CommandService>().Log += LogAsync;

                                await discordclient.LoginAsync(TokenType.Bot, config.DiscordBotToken);
                                await discordclient.StartAsync();

                                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                               
                                new GuildHandlingService(services);
                                new ReacitonHandlingService(services);
                                _raffleService = new RaffleService(services);
                                

                                await discordclient.SetGameAsync($"I like chocolate milk!");

                                await Timer.StartTimer();

                                DeveloperCollection mainDev = await DeveloperCollection.GetDevByID(235836223216680960);
                                if(mainDev == null)
                                {
                                    mainDev = new DeveloperCollection();
                                    mainDev.DiscordUserID = 235836223216680960;
                                    mainDev.DiscordName = "Dylan";
                                    await mainDev.AddNew();
                                }

                                await Task.Delay(-1);
                            }
                        }
                    }
                }

            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
            }
        }
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        public static Task CheeseLogAsync(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} {message}");

            return Task.CompletedTask;
        }
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<GuildHandlingService>()
                .AddSingleton<ReacitonHandlingService>()
                .AddSingleton<RaffleService>()
                .BuildServiceProvider();
        }
    }
}
