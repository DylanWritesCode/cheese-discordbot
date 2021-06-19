using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;

namespace CheeseBot.Services
{
    public class DatabaseService
    {
        public static MongoClient client;

        public DatabaseService(string host, string port)
        {
            client = new MongoClient($"mongodb://");
        }
    }
}
