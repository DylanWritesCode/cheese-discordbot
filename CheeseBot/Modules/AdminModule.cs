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
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("setcmdprefix")]
        [Summary("Sets the command prefix for commands.")]
        [RequireUserPermission(GuildPermission.Administrator)]
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

        [RequireNsfw]
        [Command("addimagetype")]
        [Summary("Adds image category for the r command.")]
        [RequireUserPermission(GuildPermission.Administrator)]
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

        [RequireNsfw]
        [Command("delimagetype")]
        [Summary("Deletes image category for the r command.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteImageType([Remainder] string arg)
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            if (guild.SubRedditCommands.Contains(arg))
            {
                int index = guild.SubRedditCommands.IndexOf(arg);
                guild.SubRedditCommands.RemoveAt(index);

                var lastImage = (from item in guild.LastRedditImages where item.ImageType == arg select item).FirstOrDefault();
                if(lastImage != null)
                {
                    int lastRIIndex = guild.LastRedditImages.IndexOf(lastImage);
                    guild.LastRedditImages.RemoveAt(lastRIIndex);
                }

                for(int i = 0; i < guild.RedditImageCache.Count; i++)
                {
                    if(guild.RedditImageCache[i].SubReddit.ToLower() == arg.ToLower())
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

        [Command("purge")]
        [Summary("Downloads and removes X messages from the current channel.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task PurgeAsync(int amount)
        {
            if (amount <= 0)
            {
                await ReplyAsync("The amount of messages to remove must be positive.");
                return;
            }
            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, amount).FlattenAsync();
            var filteredMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);
            var count = filteredMessages.Count();
            if (count > 0)
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(filteredMessages);
                await Context.Message.DeleteAsync();
        }
    }
}
