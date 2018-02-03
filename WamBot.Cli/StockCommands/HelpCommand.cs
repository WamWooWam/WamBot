using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamBot.Api;

namespace WamBot.Cli.StockCommands
{
    class HelpCommand : ModernDiscordCommand
    {
        public override string Name => "Help";

        public override string Description => "Well, it's help innit";

        public override string[] Aliases => new[] { "help", "tf", "?", "wtf" };

        public override string Usage => "[string alias]";

        [Run]
        public Task<CommandResult> Run(string alias = null)
        {
            DiscordEmbedBuilder embedBuilder = Context.GetEmbedBuilder();
            embedBuilder.WithFooter($"Lovingly made by @{Context.Client.CurrentApplication.Owner.Username}#{Context.Client.CurrentApplication.Owner.Discriminator} using D#+. My current prefix is `{Program.Config.Prefix}`.", Context.Client.CurrentApplication.Owner.AvatarUrl);

            if (alias == null)
            {
                embedBuilder.WithAuthor("Listing Command Categories - WamBot 3.0.0", icon_url: Context.Client.CurrentApplication.Icon);
                foreach (var asm in Program.AssemblyCommands)
                {
                    IEnumerable<DiscordCommand> availableCommands = GetAvailableCommands(asm.Value);
                    if (availableCommands.Any())
                    {
                        embedBuilder.AddField(
                            $"{asm.Key.Name} Commands", 
                            $"{asm.Key.Description}\r\n" +
                            $"`{string.Join(", ", GetAvailableCommands(asm.Value).Select(c => c.Aliases.FirstOrDefault()))}`", false);
                    }
                }
            }
            else
            {
                DiscordCommand command = GetAvailableCommands(Program.Commands).FirstOrDefault(c => c.Aliases.Contains(alias.ToLower().Trim()));
                if (command != null)
                {
                    embedBuilder.WithAuthor($"{command.Name} - WamBot 3.0.0", icon_url: Context.Client.CurrentApplication.Icon);
                    embedBuilder.AddField("Description", command.Description, true);
                    embedBuilder.AddField("Aliases", string.Join(", ", command.Aliases), true);
                    if (command.Usage != null)
                    {
                        embedBuilder.AddField("Usage", $"```cs\r\n{Program.Config.Prefix}{command.Aliases.First()} {command.Usage}\r\n```");
                    }
                }
                else
                {
                    var asm = Program.AssemblyCommands.FirstOrDefault(a => a.Key.Name.ToLower().Trim() == alias.ToLower().Trim());
                    if (asm.Value != null && asm.Key != null)
                    {
                        IEnumerable<DiscordCommand> availableCommands = GetAvailableCommands(asm.Value);
                        embedBuilder.WithAuthor($"{asm.Key.Name} Commands - WamBot 3.0.0", icon_url: Context.Client.CurrentApplication.Icon);
                        embedBuilder.WithDescription(asm.Key.Description ?? "No description provided.");
                        if (availableCommands.Any())
                        {
                            foreach (DiscordCommand c in availableCommands)
                            {
                                embedBuilder.AddField($"{c.Name} (`{c.Aliases.FirstOrDefault()}`)", c.Description, true);
                            }
                        }
                        else
                        {
                            embedBuilder.AddField("No commands available", "This command group exits, but it either contains no commands, or you don't have access to any of them");
                        }
                    }
                    else
                    {
                        embedBuilder.AddField("Command not found.", $"That command doesn't seem to exist, or you don't have permission to run it! Run `{Program.Config.Prefix}help` for a list of all commands!");
                    }
                }
            }

            return Task.FromResult<CommandResult>(embedBuilder.Build());
        }

        private IEnumerable<DiscordCommand> GetAvailableCommands(IEnumerable<DiscordCommand> commands)
        {
            IEnumerable<DiscordCommand> current = commands;
            if (Context.Message.Author.Id != Context.Client.CurrentApplication.Owner.Id)
            {
                current = current.Where(c => !c.HasAttribute<OwnerAttribute>());
            }

            current = current.Where(c => Program.CheckPermissions(Context.Message.Author, Context.Channel, c));
            current = current.Where(c => Program.CheckPermissions(Context.Guild?.CurrentMember ?? Context.Client.CurrentUser, Context.Channel, c));

            current = current.OrderBy(c => c.Name);

            return current;
        }
    }
}
