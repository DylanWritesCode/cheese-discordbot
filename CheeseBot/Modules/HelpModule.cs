using CheeseBot.Database.Collections;
using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Cheese.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            _service = service;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            GuildCollection guild = await GuildCollection.GetGuildByID(Context.Guild.Id);
            string prefix = guild.CommandPrefix.ToString();

            var builder = new EmbedBuilder()
            {
                Color = Color.Blue,
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                if (module.Name == "HelpModule" || module.Name == "DeveloperModule") continue;
                string description = null;

                foreach (var cmd in module.Commands)
                {
                    if(cmd.Aliases.First() != "help")
                    {

                    }
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases.First()} - {cmd.Summary}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(module.Name.Replace("Module", " Commands"), description);
                    /*builder.AddField(x =>
                    {
                        x.Name = module.Name.Replace("Module", " Commands");
                        x.Value = description;
                        x.IsInline = false;
                    });*/
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        /*[Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = "+";
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }*/
    }
}