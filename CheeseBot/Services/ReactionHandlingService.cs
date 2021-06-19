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
    public class ReacitonHandlingService
    {
        private readonly DiscordSocketClient _discord;

        public ReacitonHandlingService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();

            _discord.ReactionAdded += ReactionAdded;
            _discord.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            try
            {
                var channel = arg3.Channel as SocketGuildChannel;
                var message = await _discord.GetGuild(channel.Guild.Id).GetTextChannel(channel.Id).GetMessageAsync(arg3.MessageId);

                GuildCollection guild = await GuildCollection.GetGuildByID(channel.Guild.Id);
                if (guild.ActiveRaffle != null)
                {
                    Modules.RaffleModule.Raffle raff = guild.ActiveRaffle;

                    if (raff.MessageId == arg3.MessageId && arg3.Emote.Name == Modules.RaffleModule.emoji.Name && arg3.UserId != message.Author.Id)
                    {
                        raff.Participants.Remove(arg3.UserId);
                        await guild.Update();
                        await arg2.SendMessageAsync($"{arg2.GetUserAsync(arg3.UserId).Result.Mention} has left the raffle: {raff.RaffleText}");
                    }
                }
            }catch(Exception ex)
            {
                await Program.CheeseLogAsync(ex.Message + ex.StackTrace);
            }
        }

        private  async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            try
            {
                var channel = arg3.Channel as SocketGuildChannel;
                var message = await _discord.GetGuild(channel.Guild.Id).GetTextChannel(channel.Id).GetMessageAsync(arg3.MessageId);

                GuildCollection guild = await GuildCollection.GetGuildByID(channel.Guild.Id);
                if (guild.ActiveRaffle != null)
                {
                    Modules.RaffleModule.Raffle raff = guild.ActiveRaffle;
                    if (raff.MessageId == arg3.MessageId && arg3.Emote.Name == Modules.RaffleModule.emoji.Name && arg3.UserId != message.Author.Id)
                    {
                        raff.Participants.Add(arg3.UserId);
                        await guild.Update();
                        await arg2.SendMessageAsync($"{arg2.GetUserAsync(arg3.UserId).Result.Mention} has entered the raffle: {raff.RaffleText}");
                    }
                }
            }catch (Exception ex)
            {
                await Program.CheeseLogAsync(ex.Message + ex.StackTrace);
            }
        }
    }
}
