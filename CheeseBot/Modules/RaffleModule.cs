using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using CheeseBot.Services;
using System;
using System.Linq;
using CheeseBot.Database.Collections;
using System.Collections.Generic;

namespace CheeseBot.Modules
{
    public class RaffleModule : ModuleBase<SocketCommandContext>
    {
        //public static List<Raffle> ActiveRaffles = new List<Raffle>();
        public static Emoji emoji = new Emoji("✅");
        [Command("cancelraffle")]
        [Summary("Cancels an active Raffle.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RaffleCancel()
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            if(guild.ActiveRaffle != null)
            {
                await ReplyAsync($"{Context.User.Mention} has canceled raffle: {guild.ActiveRaffle.RaffleText}");
                guild.ActiveRaffle = null;
                await guild.Update();
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention} there is no active raffle to cancel.");
            }
        }

        [Command("startraffle")]
        [Summary("Starts a Raffle! rafflestart <time-in-minutes> <description>")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RaffleStart([Remainder] string arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);

            if(guild.ActiveRaffle != null)
            {
                await ReplyAsync($"You can not begin another raffle until the active one has ended. (Current Raffle Ends: {guild.ActiveRaffle.EndTime}) {Context.User.Mention}");
                return;
            }

            string[] values = arg.Split(' ');

            bool minSuccess = Int32.TryParse(values[0], out int raffleMinutes);
            if(!minSuccess)
            {
                await ReplyAsync("Invalid minute format. Please select a number 5-360");
            }
            
            string embedName = arg.Substring(arg.IndexOf(' '), arg.Length-arg.IndexOf(' '));

            var builder = new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = embedName,
                Description = "Select :white_check_mark: to enter the raffle.",
                Url = "https://discordapp.com",
                //Footer = { IconUrl = "https://i.imgur.com/voaHjO6.png", Text = $"Created By Dylan| Raffle Ends: {DateTime.Now}"},
                ThumbnailUrl = "https://i.imgur.com/voaHjO6.png"

            };

            builder.Author = new EmbedAuthorBuilder();
            builder.Author.Name = "New Raffle";

            builder.Footer = new EmbedFooterBuilder();
            builder.Footer.Text = $"Created by {Context.User} | Raffle Ends: {DateTime.Now.AddMinutes(raffleMinutes)}";

            var botMsg = await ReplyAsync("", false, builder.Build());

            Raffle newRaffle = new Raffle();
            newRaffle.GuildId = Context.Guild.Id;
            newRaffle.OwnerId = Context.User.Id;
            newRaffle.MessageId = botMsg.Id;
            newRaffle.ChannelId = botMsg.Channel.Id;
            newRaffle.RaffleText = embedName;
            newRaffle.StartTime = DateTime.UtcNow;
            Console.WriteLine(DateTime.UtcNow.AddMinutes(raffleMinutes));
            newRaffle.EndTime = DateTime.UtcNow.AddMinutes(raffleMinutes);

            guild.ActiveRaffle = newRaffle;
            await guild.Update();

            await botMsg.AddReactionAsync(emoji);
      
        }

        [Command("rafflehistory")]
        [Summary("Displays the last 10 raffles and their winners!")]
        public async Task Last10Raffles()
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);

            if (guild.RaffleHistory.Count > 0)
            {
                string msg = "";
                var rhistory = (from row in guild.RaffleHistory
                                orderby row.RaffleDate descending
                                select row);
                int count = 0;
                if (rhistory != null)
                {
                    foreach (RaffleHistory rh in rhistory)
                    {
                        if (!string.IsNullOrEmpty(msg)) msg += "\n";

                        msg += $"{rh.DiscordUsername} won {rh.Raffle} on {rh.RaffleDate}";
                        count++;
                        if (count == 10) break;
                    }
                    await ReplyAsync($"Raffle History\n```{msg}```");
                }
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention} no raffle history found. :frowning:");
            }
        }
            public void Winner()
        {
            
        }
        /* [RequireNsfw]
         [Command("Raffle")]
         [Summary("Displays a list of available image types on this server.")]
         [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
         public async Task GuildImageList()
         {

         }*/
        public class Raffle
        {
            public ulong GuildId { get; set; }
            public ulong OwnerId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong MessageId { get; set; }
            public string RaffleText { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }

            public List<ulong> Participants = new List<ulong>();
        }

        public class RaffleHistory
        {
            public string DiscordUsername { get; set; }
            public string Raffle { get; set; }
            public DateTime RaffleDate { get; set; }
        }
    }
}
