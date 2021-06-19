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
    public class GeneralModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }

        [Command("ping")]
        [Summary("Pong!")]
    //    [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        /*[RequireNsfw]
        [Command("cat")]
        public async Task CatAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetCatPictureAsync();
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "cat.png");

        }*/
        [RequireNsfw]
        [Command("imagetypes")]
        [Summary("Displays a list of available image types on this server.")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public async Task GuildImageList()
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            if(guild.SubRedditCommands.Count == 0)
            {
                await ReplyAsync($"This guild has not set any Images yet! {Context.User.Mention}");
                return;
            }
            var builder = new EmbedBuilder()
            {
                Color = Color.Blue,
                Description = $"Availabe Image Types (This server image Limit: {guild.ImageTypeLimit})"
            };

            string images = "";
            foreach(string cmd in guild.SubRedditCommands)
            {
                if(string.IsNullOrEmpty(images))
                {
                    images += $"{cmd}";
                }
                else
                {
                    images += $", {cmd}";
                }
            }

            builder.AddField($"Usage: {guild.CommandPrefix}r <image>", images);
            await ReplyAsync("", false, builder.Build());
        }


        [RequireNsfw]
        [Command("r")]
        [Summary("Generates a random image. Usage: r <imagetype>")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public async Task RedditPorn([Remainder] string arg)
        {
            arg = arg.ToLower();

            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);

            string subr = (from subrCmd in
                                guild.SubRedditCommands
                           where subrCmd.ToLower() == arg
                           select subrCmd).FirstOrDefault();

            if (subr != null)
            {
                if (DateTime.Now >= guild.LastRedditImageRefresh.AddDays(1))
                {
                    guild.RedditImageCache.Clear();
                    guild.LastRedditImageRefresh = DateTime.Now;
                    await guild.Update();
                }

                var redditImages = (from ri in
                                       guild.RedditImageCache
                                    where ri.SubReddit.ToLower() == arg.ToLower()
                                    select ri);

                if (redditImages.Count() <= 0)
                {
                    var imageTask = await RedditModule.LoadImages(arg, guild);
                    imageTask.Wait();

                    redditImages = (from ri in
                                        guild.RedditImageCache
                                    where ri.SubReddit.ToLower() == arg
                                    select ri);
                }

                GuildCollection.LastRedditImageDisplayed lastImage = null;

                if(guild.LastRedditImages != null)
                {
                    lastImage = (from li in
                                            guild.LastRedditImages
                                     where li.ImageType.ToLower() == arg
                                     select li).FirstOrDefault();
                }
                if (redditImages.Count() <= 0) return;

                if (lastImage != null)
                {
                    bool lastImageIDFound = false;
                    RedditModule.RedditImage displayImage = null;

                    if(redditImages.Last().ImageID != lastImage.ImageID)
                    {
                        foreach (var image in redditImages)
                        {
                            if (lastImageIDFound)
                            {
                                displayImage = image;
                                break;
                            }
                            if (lastImage.ImageID == image.ImageID)
                            {
                                lastImageIDFound = true;
                            }
                        }
                    }

                    if (lastImageIDFound)
                    {
                        string filename = Path.GetFileName(displayImage.ImageURL);

                        var stream = await PictureService.GetRedditPictureAsync(displayImage.ImageURL);
                        stream.Seek(0, SeekOrigin.Begin);
                        await Context.Channel.SendFileAsync(stream, filename);

                        lastImage.ImageID = displayImage.ImageID;
                        await guild.Update();
                    }
                    else
                    {
                        foreach (var image in redditImages)
                        {
                            string filename = Path.GetFileName(image.ImageURL);

                            var stream = await PictureService.GetRedditPictureAsync(image.ImageURL);
                            stream.Seek(0, SeekOrigin.Begin);
                            await Context.Channel.SendFileAsync(stream, filename);

                            lastImage.ImageID = image.ImageID;
                            await guild.Update();
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var image in redditImages)
                    {
                        string filename = Path.GetFileName(image.ImageURL);
                        var stream = await PictureService.GetRedditPictureAsync(image.ImageURL);
                        stream.Seek(0, SeekOrigin.Begin);
                        await Context.Channel.SendFileAsync(stream, filename);

                        GuildCollection.LastRedditImageDisplayed lastRedditImageDisplayed = new GuildCollection.LastRedditImageDisplayed
                        {
                            ImageID = image.ImageID,
                            ImageType = image.SubReddit
                        };

                        if (guild.LastRedditImages == null)
                        {
                            List<GuildCollection.LastRedditImageDisplayed> lastImagesList = new List<GuildCollection.LastRedditImageDisplayed>();
                            lastImagesList.Add(lastRedditImageDisplayed);
                            guild.LastRedditImages = lastImagesList;
                        }
                        else
                        {
                            guild.LastRedditImages.Add(lastRedditImageDisplayed);
                        }

                        await guild.Update();
                        break;
                    }
                }
                
            }
            else
            {
                await ReplyAsync($"{arg} is not a valid image type! Check {guild.CommandPrefix}imagetypes {Context.User.Mention}");
            }
        }


        // Get info on a user, or the user who invoked the command if one is not specified
       /* [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        // Ban a user
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        // make sure the user invoking the command can ban
        [RequireUserPermission(GuildPermission.BanMembers)]
        // make sure the bot itself can ban
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }

        [Command("echo")]
        public Task EchoAsync([Remainder] string text)
            => ReplyAsync('\u200B' + text);

        [Command("list")]
        public Task ListAsync(params string[] objects)
            => ReplyAsync("You listed: " + string.Join("; ", objects));

        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");*/
    }
}
