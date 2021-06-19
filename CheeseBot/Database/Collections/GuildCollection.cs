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
    public class GuildCollection
    {
        public static IMongoCollection<GuildCollection> collection; //Initalized from Program.cs

        public ObjectId Id { get; set; }
        public ulong GuildID { get; set; }
        public ulong OwnerID { get; set; }
        public string GuildName { get; set; }
        public char CommandPrefix { get; set; }
        public DateTime JoinDate { get; set; }

        public int MemberCount { get; set; }
        public int ImageTypeLimit { get; set; }
        public List<string> SubRedditCommands = new List<string>();
        public DateTime LastRedditImageRefresh = DateTime.Now;
        public List<Modules.RedditModule.RedditImage> RedditImageCache = new List<Modules.RedditModule.RedditImage>();
        public List<LastRedditImageDisplayed> LastRedditImages { get; set; }

        public Modules.RaffleModule.Raffle ActiveRaffle { get; set; }
        public List<Modules.RaffleModule.RaffleHistory> RaffleHistory = new List<Modules.RaffleModule.RaffleHistory>();
        

        public async Task AddNew()
        {
            await collection.InsertOneAsync(this);
            await Program.CheeseLogAsync($"[MongoDb] Guild:{GuildID} has been added.");
        }

        public async Task Delete()
        {
            if(Id != null)
            {
                var filter = Builders<GuildCollection>.Filter.Eq("_id", Id);
                await collection.DeleteOneAsync(filter);
                await Program.CheeseLogAsync($"[MongoDb] Guild:{GuildID} has been deleted.");
            }
        }
        
        public async Task Update()
        {
            if(Id != null)
            {
                var filter = Builders<GuildCollection>.Filter.Eq("_id", Id);
                var result = await collection.ReplaceOneAsync(filter, this);
                if(result.IsModifiedCountAvailable)
                {
                    await Program.CheeseLogAsync($"[MongoDb] Guild:{GuildID} has been updated.");
                }
            }
        }
        public static async Task<GuildCollection> GetGuildByID(ulong guildID)
        {
            return await collection.Find(g => g.GuildID == guildID).FirstOrDefaultAsync();
        }

        public static async Task<List<GuildCollection>> GetAllGuilds()
        {
            return await collection.FindSync(new BsonDocument()).ToListAsync();
        }
        public class LastRedditImageDisplayed
        {
            public string ImageType { get; set; }
            public string ImageID { get; set; }
        }
    }
}
