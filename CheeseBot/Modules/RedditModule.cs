using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using RedditSharp;
using RedditSharp.Things;
using CheeseBot.Database.Collections;

namespace CheeseBot.Modules
{
    public class RedditModule
    {
        //public static DateTime LastLoaded = DateTime.Now;
        //public static List<RedditImage> RedditImages = new List<RedditImage>();

        public class RedditImage
        {
            public string ImageID { get; set; }
            public string ImageURL { get; set; }
            public string SubReddit { get; set; }

        }

        public static async Task<Task> LoadImages(string subreddit, GuildCollection guild)
        {
            List<RedditImage> guildRedditImages = guild.RedditImageCache;

            var subrImages = (from img in
                                  guildRedditImages
                              where img.SubReddit.ToLower() == subreddit.ToLower()
                              select img);

            if(subrImages.Count() > 0)
            {
                for(int i = 0; i < guildRedditImages.Count; i++)
                {
                    if(guildRedditImages[i].SubReddit == subreddit)
                    {
                        guildRedditImages.RemoveAt(i);
                    }
                }
                guild.RedditImageCache = guildRedditImages;
                await guild.Update();
            }


            string slashRSubreddit = $"/r/{subreddit}";
            int amount = 200;
            int processed = 0;

            var reddit = new Reddit();
            var sub = await reddit.GetSubredditAsync(slashRSubreddit);

            using (var posts = sub.GetPosts().GetEnumerator(1000, 1000))
            {
                while (posts.MoveNext().Result)
                {
                    string url = posts.Current.Url.ToString();
                    string imageID = posts.Current.Id;

                    string fileExt = url.Substring(url.Length - 4, 4);
                    if (fileExt == ".jpg" || fileExt == ".png" || fileExt == ".gif" || fileExt == ".gifv")
                    {
                        processed++;
                        RedditImage newImage = new RedditImage();
                        newImage.ImageID = imageID;
                        newImage.ImageURL = url;
                        newImage.SubReddit = subreddit;
                        guild.RedditImageCache.Add(newImage);
                        if (processed == amount) break;
                    }
                }
            }
             await guild.Update();
            return Task.CompletedTask;
        }
    }
}
