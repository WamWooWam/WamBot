using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace WamBot.Commands
{
    public class HelpCommand : BaseCommandModule
    {
        [Command]
        [Description("Help! I've fallen, and I need @someone!")]
        public async Task HelpAsync(CommandContext ctx, [RemainingText] string command = null)
        {
            var builder = ctx.GetEmbedBuilder("Help");

            try
            {
                if (string.IsNullOrWhiteSpace(command))
                {
                    builder.WithDescription("Listing all commands");

                    var groups = ctx.CommandsNext.RegisteredCommands
                        .Select(c => c.Value)
                        .OfType<CommandGroup>()
                        .ToList();

                    var looseCommands = ctx.CommandsNext.RegisteredCommands
                        .Select(c => c.Value)
                        .Where(c => !groups.Any(g => g.Children.Contains(c)))
                        .Except(groups)
                        .ToList();

                    AppendGroups(ctx, builder, groups);
                    AppendCommands(ctx, command, builder, looseCommands);
                }
                else
                {
                    var found = ctx.CommandsNext.RegisteredCommands.FirstOrDefault(c => c.Key.ToLowerInvariant() == command.ToLowerInvariant()).Value ?? ctx.CommandsNext.FindCommand(command, out _);

                    if (found is CommandGroup group)
                    {
                        builder.WithTitle(group.QualifiedName);
                        if (group.Description != null)
                            builder.WithDescription(group.Description);

                        var subGroups = group.Children.OfType<CommandGroup>();
                        AppendGroups(ctx, builder, subGroups);

                        var subCommands = group.Children.Except(subGroups);
                        AppendCommands(ctx, command, builder, subCommands);
                    }
                    else
                    {
                        builder.WithTitle(found.QualifiedName);
                        if (found.Description != null)
                            builder.WithDescription(found.Description);


                        if (found.Aliases.Any())
                            builder.AddField("Aliases", $"`{string.Join(", ", found.Aliases)}`", true);

                        for (var i = 0; i < found.Overloads.Count; i++)
                        {
                            var overload = found.Overloads[i];
                            var stringBuilder = new StringBuilder();
                            stringBuilder.AppendLine("```");
                            stringBuilder.Append(ctx.Prefix);
                            stringBuilder.Append(found.QualifiedName);

                            foreach (var item in overload.Arguments)
                            {
                                if (item.IsCatchAll)
                                {
                                    stringBuilder.Append(" ...");
                                    break;
                                }

                                stringBuilder.Append(" ");

                                if (item.IsOptional)
                                    stringBuilder.Append("[");

                                stringBuilder.Append(ctx.CommandsNext.GetUserFriendlyTypeName(item.Type));
                                stringBuilder.Append(" ");
                                stringBuilder.Append(item.Name);

                                if (item.IsOptional)
                                    stringBuilder.Append("]");
                            }

                            stringBuilder.AppendLine();
                            stringBuilder.Append("```");

                            builder.AddField($"Usage #{i + 1}", stringBuilder.ToString(), false);
                        }
                    }
                }
            }
            catch
            {
                builder.WithTitle("Command not found!")
                       .WithDescription($"That command doesn't seem to exist, or you don't have permission to run it! Run {ctx.Prefix}help for a list of all commands!");

                if (command != null)
                {
                    var potentials = ctx.CommandsNext.RegisteredCommands.Where(c => c.Key.Distance(command) <= 3);
                    if (potentials.Any())
                    {
                        builder.AddField("Did you mean?", $"`{string.Join(", ", potentials.Select(c => c.Value.QualifiedName))}`");
                    }
                }
            }

            await ctx.RespondAsync(embed: builder.Build());
        }

        private static void AppendGroups(CommandContext ctx, DiscordEmbedBuilder builder, IEnumerable<CommandGroup> subGroups)
        {
            if (subGroups.Any())
            {
                //builder.AddField("Groups", $"Type {ctx.Prefix}help <group_name> for more info!");

                foreach (var subGroup in subGroups.FilterCommands(ctx))
                {
                    var stringBuilder = new StringBuilder(subGroup.Description);
                    stringBuilder.AppendLine();
                    stringBuilder.Append("`");

                    foreach (var subCommand in subGroup.Children.FilterCommands(ctx))
                    {
                        stringBuilder.Append(subCommand.Aliases.FirstOrDefault() ?? subCommand.Name);
                        stringBuilder.Append(" ");
                    }

                    stringBuilder.Append("`");

                    builder.AddField(subGroup.Name, stringBuilder.ToString());
                }
            }
        }

        private static void AppendCommands(CommandContext ctx, string command, DiscordEmbedBuilder builder, IEnumerable<Command> subCommands)
        {
            if (subCommands.Any())
            {
                //builder.AddField("Commands", $"Type {ctx.Prefix}help {command} <group_name> for more info!");

                foreach (var subCommand in subCommands.FilterCommands(ctx))
                {
                    builder.AddField($"{ctx.Prefix}{subCommand.QualifiedName}", subCommand.Description, true);
                }
            }
        }
    }
}
