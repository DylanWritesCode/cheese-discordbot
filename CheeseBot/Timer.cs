using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CheeseBot
{
    public class Timer
    {
        private static bool IsTimerStopped = true;

        public static void StopTimer()
        {
            IsTimerStopped = true;
        }

        public static async Task StartTimer()
        {
            await Program.CheeseLogAsync("Reddit Image Refresh Timer has started!");
            IsTimerStopped = false;
            while(!IsTimerStopped)
            {
                try
                {
                    Console.WriteLine("Timer Elapsed");
                    List<Database.Collections.GuildCollection> allGuilds = await Database.Collections.GuildCollection.GetAllGuilds();
                    foreach (Database.Collections.GuildCollection guild in allGuilds)
                    {
                        if (guild.ActiveRaffle != null)
                        {
                            if (guild.ActiveRaffle.EndTime <= DateTime.UtcNow)
                            {
                                await Program._raffleService.GetRaffleWinner(guild);
                            }
                        }

                        //Reddit Image Cache Refresh
                        if (guild.RedditImageCache.Count > 0 && DateTime.UtcNow > guild.LastRedditImageRefresh.AddDays(1))
                        {
                            await Program.CheeseLogAsync($"Refreshing {guild.GuildName} Reddit Image Cache!");
                            guild.RedditImageCache.Clear();
                            guild.LastRedditImageRefresh = DateTime.UtcNow;
                            guild.LastRedditImages.Clear();
                            await guild.Update();

                            if (guild.SubRedditCommands.Count > 0)
                            {
                                foreach (string cmd in guild.SubRedditCommands)
                                {
                                    await Modules.RedditModule.LoadImages(cmd, guild);
                                }
                            }
                            await Program.CheeseLogAsync($"{guild.GuildName} image cache refreshed!");
                        }
                    }
                    await Task.Delay(5000);
                } catch (Exception ex)
                {
                    await Program.CheeseLogAsync(ex.Message + ex.StackTrace);
                }
            }
        }

        /*public static void StartTimer()
        {
            IsTimerStopped = false;

            Task.Run(async () =>
            {
                while (!IsTimerStopped)
                {
                    List<Database.Collections.GuildCollection> allGuilds = new List<Database.Collections.GuildCollection>();
                    foreach(Database.Collections.GuildCollection guild in allGuilds)
                    {
                        if(guild.RedditImageCache.Count > 0 && DateTime.Now.AddDays(1) < guild.LastRedditImageRefresh)
                        {
                            Console.WriteLine($"Refreshing {guild.GuildName} Reddit Image Cache!");
                            guild.RedditImageCache.Clear();
                            guild.LastRedditImageRefresh = DateTime.Now;
                            await guild.Update();

                            if(guild.SubRedditCommands.Count > 0)
                            {
                                foreach(string cmd in guild.SubRedditCommands)
                                {
                                    await Modules.RedditModule.LoadImages(cmd, guild);
                                }
                            }
                            Console.WriteLine($"{guild.GuildName} image cache refreshed!");
                        }
                    }
                    
                    await Task.Delay(5000);
                }
            });
        }*/
    }
}
