using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using CheeseBot.Services;
using CheeseBot.Database.Collections;

namespace CheeseBot.Services
{
    public class RaffleService
    {
        private readonly DiscordSocketClient _discord;
        public static int TotalServers = 0;
        public RaffleService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
        }

        public async Task GetRaffleWinner(GuildCollection guild)
        {
            try
            {
                Modules.RaffleModule.Raffle raffle = guild.ActiveRaffle;
                guild.ActiveRaffle = null;
                await guild.Update();

                if (_discord.ConnectionState != ConnectionState.Connected || !_discord.GetGuild(guild.GuildID).IsConnected) return;
                Console.WriteLine(_discord.ConnectionState);
                var dguild = _discord.GetGuild(guild.GuildID);
                var channel = _discord.GetGuild(guild.GuildID).GetTextChannel(raffle.ChannelId);

                Modules.RaffleModule.RaffleHistory raffleHistory = new Modules.RaffleModule.RaffleHistory();
                raffleHistory.DiscordUsername = "";
                raffleHistory.Raffle = raffle.RaffleText;
                raffleHistory.RaffleDate = DateTime.UtcNow;

                if (raffle.Participants == null || raffle.Participants.Count == 0)
                {
                    await channel.SendMessageAsync($"Nobody has won the raffle! No one signed up. Raffle Ended.");
                    raffleHistory.DiscordUsername = "Nobody";
                }

                if (raffle.Participants.Count < 2)
                {
                    await channel.SendMessageAsync($"Nobody has won the raffle! Not enough participants. Raffle Ended");
                    raffleHistory.DiscordUsername = "Nobody";
                }
                else
                {
                    var random = new Random();
                    var index = random.Next(raffle.Participants.Count - 1);
                    var user = dguild.GetUser(raffle.Participants[index]);

                    await channel.SendMessageAsync($"{user.Mention} has won the raffle `{raffle.RaffleText}`!");

                    raffleHistory.DiscordUsername = user.ToString();
                }

                await Program.CheeseLogAsync($"{guild.Id} raffle {raffle.RaffleText} has ended.");

                guild.RaffleHistory.Add(raffleHistory);
                await guild.Update();

            } catch (Exception ex)
            {
                await Program.CheeseLogAsync(ex.Message + ex.StackTrace);
            }
        }
    }
}
