using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using CheeseBot.Services;
using System;
using System.Linq;
using CheeseBot.Database.Collections;
using System.Collections.Generic;

namespace CheeseBot.Modules
{
    public class DeveloperModule : ModuleBase<SocketCommandContext>
    {
        [RequireNsfw]
        [Command("dev")]
        [Summary("Developer command")]
        public async Task DeveloperCommandCaller([Remainder] string arg)
        {
            DeveloperCollection dev = await DeveloperCollection.GetDevByID(Context.User.Id);
            if(dev == null)return;
            if(dev.DiscordUserID != Context.User.Id) return;

            string[] splitstr = arg.Split(' ');

            switch (splitstr[0])
            {
                case "reloadall":
                    await ReplyAsync($"Reloading all images for guild...{Context.Message.Author.Mention}");
                    GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
                    guild.LastRedditImages.Clear();
                    guild.LastRedditImageRefresh = DateTime.Now;
                    guild.RedditImageCache.Clear();
                    await guild.Update();

                    foreach (string cmd in guild.SubRedditCommands)
                    {
                        await RedditModule.LoadImages(cmd, guild);
                    }
                    await ReplyAsync($"Images reloaded!{Context.Message.Author.Mention}");
                    break;
                case "addimagetype":
                    if (splitstr.Count() < 2) {await ReplyAsync($"Not enough arguments {Context.User.Mention}"); return; }

                    await AddImageType(splitstr[1]);
                    break;

                case "delimagetype":
                    if (splitstr.Count() < 2) { await ReplyAsync($"Not enough arguments {Context.User.Mention}"); return; }
                    await DeleteImageType(splitstr[1]);
                    break;
                case "adddev":
                    if (splitstr.Count() < 2) { await ReplyAsync($"Not enough arguments {Context.User.Mention}"); return; }
                    DeveloperCollection dev2 = await DeveloperCollection.GetDevByID(Convert.ToUInt64(splitstr[1]));
                    if (dev2 == null)
                    {
                        bool foundUser = false;
                        foreach(var user in Context.Guild.Users)
                        {
                            if(user.Id == Convert.ToUInt64(splitstr[1]))
                            {
                                DeveloperCollection newDev = new DeveloperCollection();
                                newDev.DiscordUserID = Convert.ToUInt64(user.Id);
                                newDev.DiscordName = user.Username;
                                await newDev.AddNew();
                                await ReplyAsync($"Developer added.");
                            }
                        }
                        if(!foundUser)
                        {
                            await ReplyAsync($"User not found in this guild.");
                            return;
                        }
                    }
                    else
                    {
                        await ReplyAsync("Developer already added!");
                    }
                    break;
                case "deldev":
                    if (splitstr.Count() < 2) { await ReplyAsync($"Not enough arguments {Context.User.Mention}"); return; }
                    DeveloperCollection dev3 = await DeveloperCollection.GetDevByID(Convert.ToUInt64(splitstr[1]));
                    if (dev3 != null)
                    {
                        await dev3.Delete();
                        await ReplyAsync($"Developer deleted.");
                    }
                    else
                    {
                        await ReplyAsync("Developer does not exist!");
                    }

                    break;
            }
        }
        public async Task SetCommandPrefix([Remainder] string arg)
        {
            if (arg.Length == 1 && Util.StringHasSpecialChars(arg))
            {
                GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
                guild.CommandPrefix = Convert.ToChar(arg);
                await guild.Update();
                await ReplyAsync($"The command prefix for this guild has been updated to '{arg}' {Context.User.Mention}");
            }
            else
            {
                await ReplyAsync($"Invalid command prefix! It must be 1 character and a special character. {Context.User.Mention}");
            }
        }
        public async Task AddImageType([Remainder] string arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            if (arg.Contains(" ") || arg.Contains("/") || arg.Contains("."))
            {
                await ReplyAsync($"Invalid characters in image type! {Context.User.Mention}");
                return;
            }
            if (guild.SubRedditCommands.Count >= guild.ImageTypeLimit)
            {
                await ReplyAsync($"You have reached your limit of {guild.ImageTypeLimit} image types {Context.User.Mention}");
                return;
            }

            await ReplyAsync($"adding image type {arg}... {Context.User.Mention}");

            var redditImages = (from ri in
                        guild.RedditImageCache
                                where ri.SubReddit.ToLower() == arg.ToLower()
                                select ri);

            if (redditImages.Count() <= 0)
            {
                var imageTask = await RedditModule.LoadImages(arg, guild);
                imageTask.Wait();
            }

            guild.SubRedditCommands.Add(arg);
            await guild.Update();
            await ReplyAsync($"{arg} has been added as an image type! {Context.User.Mention}");
        }
        public async Task DeleteImageType([Remainder] string arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            if (guild.SubRedditCommands.Contains(arg))
            {
                int index = guild.SubRedditCommands.FindIndex(a => a == arg);
                guild.SubRedditCommands.RemoveAt(index);
                for (int i = 0; i < guild.RedditImageCache.Count; i++)
                {
                    if (guild.RedditImageCache[i].SubReddit.ToLower() == arg.ToLower())
                    {
                        guild.RedditImageCache.RemoveAt(i);
                    }
                }
                await guild.Update();
                await ReplyAsync($"{arg} has been removed as an image type! {Context.User.Mention}");
            }
            else
            {
                await ReplyAsync($"{arg} does not exist as an image type for your Guild! {Context.User.Mention}");
            }
        }
    }
}
