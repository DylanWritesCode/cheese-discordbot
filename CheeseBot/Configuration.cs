using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace CheeseBot
{
    public class Configuration
    {
        private static readonly string _configPath = $"{AppDomain.CurrentDomain.BaseDirectory}/config.json";
        public string MongoHost { get; set; }
        public string MongoPort { get; set; }
        public string MongoDatabase { get; set; }
        public string MongoUsername { get; set; }
        public string MongoPassword { get; set; }
        public string DiscordBotToken { get; set; }

        public static bool CreateSampleConfigFile()
        {
            if (File.Exists(_configPath)) return false;

            Configuration cfg = new Configuration();
            cfg.DiscordBotToken = "";
            cfg.MongoHost = "";
            cfg.MongoPort = "";
            cfg.DiscordBotToken = "";
            cfg.MongoDatabase = "";
            cfg.MongoUsername = "";
            cfg.MongoPassword = "";

            string jsonObject = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            File.WriteAllText(_configPath, jsonObject);
            return true;
        }

        public Configuration LoadConfiguration()
        {
            if (!File.Exists(_configPath))
            {
                Console.WriteLine("Config.json not found!");
                return null;
            }

            Configuration configFile = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(_configPath));
            if(string.IsNullOrEmpty(configFile.DiscordBotToken))
            {
                Console.WriteLine("Please set the Discord Bot Token value in your config.json file.");
                return null;
            }
            else
            {
                return configFile;
            }
        }
    }
}
