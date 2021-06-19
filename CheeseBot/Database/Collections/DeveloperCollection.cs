using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;

namespace CheeseBot.Database.Collections
{
    public class DeveloperCollection
    {
        public static IMongoCollection<DeveloperCollection> collection; //Initalized from Program.cs

        public ObjectId Id { get; set; }
        public ulong DiscordUserID { get; set; }
        public string DiscordName { get; set; }

        public async Task AddNew()
        {
            await collection.InsertOneAsync(this);
            await Program.CheeseLogAsync($"[MongoDb] Developer:{DiscordName} has been added.");
        }

        public async Task Delete()
        {
            if (Id != null)
            {
                var filter = Builders<DeveloperCollection>.Filter.Eq("_id", Id);
                await collection.DeleteOneAsync(filter);
                await Program.CheeseLogAsync($"[MongoDb] Developer:{DiscordName} has been deleted.");
            }
        }

        public async Task Update()
        {
            if (Id != null)
            {
                var filter = Builders<DeveloperCollection>.Filter.Eq("_id", Id);
                var result = await collection.ReplaceOneAsync(filter, this);
                if (result.IsModifiedCountAvailable)
                {
                    await Program.CheeseLogAsync($"[MongoDb] Developer:{DiscordName} has been updated.");
                }
            }
        }
        public static async Task<DeveloperCollection> GetDevByID(ulong devID)
        {
            return await collection.Find(g => g.DiscordUserID == devID).FirstOrDefaultAsync();
        }

    }
}
