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
    public class GuildHandlingService
    {
        private readonly DiscordSocketClient _discord;
        public static int TotalServers = 0;
        private static int TotalMembers = 0;

        public GuildHandlingService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();

            _discord.JoinedGuild += Discord_JoinedGuild;
            _discord.GuildAvailable += _discord_GuildAvailable;
            _discord.LeftGuild += Discord_LeftGuild;
            _discord.UserJoined += Discord_UserJoined;
            _discord.UserLeft += Discord_UserLeft;
        }

        public Task updateMemberCount()
        {
            TotalMembers = 0;
            foreach(var guild in _discord.Guilds)
            {
                TotalMembers += guild.MemberCount;
            }
            return Task.CompletedTask;
        }

        private async Task Discord_UserLeft(SocketGuildUser arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(arg.Guild.Id);
            guild.MemberCount = arg.Guild.MemberCount;
            await guild.Update();

            await updateMemberCount();
            await _discord.SetGameAsync($"{_discord.Guilds.Count} servers | {TotalMembers} users");
        }

        private async Task Discord_UserJoined(SocketGuildUser arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(arg.Guild.Id);
            guild.MemberCount = arg.Guild.MemberCount;
            await guild.Update();

            await updateMemberCount();
            await _discord.SetGameAsync($"{_discord.Guilds.Count} servers | {TotalMembers} users");
        }

        private async Task _discord_GuildAvailable(SocketGuild arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(arg.Id);
            if (guild == null)
            {
                GuildCollection newGuild = new GuildCollection();
                newGuild.GuildID = arg.Id;
                newGuild.GuildName = arg.Name;
                newGuild.JoinDate = DateTime.Now;
                newGuild.OwnerID = arg.OwnerId;
                newGuild.CommandPrefix = '+';
                newGuild.ImageTypeLimit = 10;
                await newGuild.AddNew();
            }
            else
            {
                guild.MemberCount = arg.MemberCount;
                await guild.Update();
            }

            await updateMemberCount();
            await _discord.SetGameAsync($"{_discord.Guilds.Count} servers | {TotalMembers} users");
        }
        private async Task Discord_LeftGuild(SocketGuild arg)
        {
            Console.WriteLine($"Guild Left! {arg.Name} ID: {arg.Id}");
            GuildCollection guild = await GuildCollection.GetGuildByID(arg.Id);
            if (guild != null)
            {
                await guild.Delete();
            }

            await updateMemberCount();
            await _discord.SetGameAsync($"{_discord.Guilds.Count} servers | {TotalMembers} users");
        }

        private async Task Discord_JoinedGuild(SocketGuild arg)
        {
            Console.WriteLine($"New Guild Joined! {arg.Name} ID: {arg.Id}");

            if (await GuildCollection.GetGuildByID(arg.Id) == null)
            {
                GuildCollection newGuild = new GuildCollection();
                newGuild.GuildID = arg.Id;
                newGuild.GuildName = arg.Name;
                newGuild.JoinDate = DateTime.Now;
                newGuild.OwnerID = arg.OwnerId;
                newGuild.CommandPrefix = '+';
                newGuild.ImageTypeLimit = 5;
                await newGuild.AddNew();
            }

            await updateMemberCount();
            await _discord.SetGameAsync($"{_discord.Guilds.Count} servers | {TotalMembers} users");
        }
    }
}
