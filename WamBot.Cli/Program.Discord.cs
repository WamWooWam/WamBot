using DSharpPlus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WamBot.Api;
using WamWooWam.Core;
using DSharpPlus.EventArgs;
using System.Net.Http;
using DSharpPlus.Entities;

using ConsoleDraw;
using ConsoleDraw.UI;
using WamBot.Cli.StockCommands;
using WamWooWam.Core.Serialisation;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;

namespace WamBot.Cli
{
    partial class Program
    {
        static DiscordDmChannel _ownerDm;
        static List<ulong> ProcessedMessageIds = new List<ulong>();

        private static async Task Client_Ready(ReadyEventArgs e)
        {
            _appLogArea.WriteLine("Ready!");
            _statusButton.Text = $"{Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}";
            await UpdateStatusAsync();
        }

        private static Task Client_Heartbeated(HeartbeatEventArgs e)
        {
            _pingArea.Text = $"Ping: {e.Ping}ms, Chk: {e.IntegrityChecksum}";
            return Task.CompletedTask;
        }

        private static async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            _appLogArea.WriteLine($"Guild {e.Guild.Name} is now available");
            TelemetryClient?.TrackEvent($"GuildAvailable", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });

            Config.SeenGuilds.Add(e.Guild.Id);
            if (Config.DisallowedGuilds.Contains(e.Guild.Id))
            {
                DiscordChannel c = await GetFirstChannelAsync(e.Guild);
                await c.SendMessageAsync(":middle_finger:");
                await e.Guild.LeaveAsync();
            }
            else
            {
                await UpdateOwnerDm();
            }
        }

        private static async Task Client_GuildCreated(GuildCreateEventArgs e)
        {
            _appLogArea.WriteLine($"Guild {e.Guild.Name} has been added!");
            TelemetryClient?.TrackEvent($"GuildJoin", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });

            if (Config.DisallowedGuilds.Contains(e.Guild.Id))
            {
                DiscordChannel c = await GetFirstChannelAsync(e.Guild);
                await c.SendMessageAsync(":middle_finger:");
                await e.Guild.LeaveAsync();
            }
            else
            {
                await SendWelcomeMessage(e.Guild);
            }
        }

        private static Task Client_GuildUnavailable(GuildDeleteEventArgs e)
        {
            _appLogArea.WriteLine($"Guild {e.Guild.Name} is now unavailable");
            TelemetryClient?.TrackEvent($"GuildUnavailable", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });
            return Task.CompletedTask;
        }

        private static Task Client_GuildDeleted(GuildDeleteEventArgs e)
        {
            _appLogArea.WriteLine($"Guild {e.Guild.Name} has been removed (kick/ban)");
            TelemetryClient?.TrackEvent($"GuildLeave", new Dictionary<string, string> { { "id", e.Guild.Id.ToString() }, { "name", e.Guild.Name } });
            return Task.CompletedTask;
        }

        private static async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            await ProcessMessage(e.Message, e.Author, e.Channel);
        }

        private static async Task Client_MessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Channel != null && e.Message != null && e.Author != null)
            {
                await ProcessMessage(e.Message, e.Author, e.Channel);
            }
        }

        private static void DebugLogger_LogMessageReceived(object sender, DebugLogMessageEventArgs e)
        {
            _dSharpPlusLogArea.WriteLine($"[{e.Level}][{e.Application}] {e.Message}");
        }

        private static Task Client_SocketErrored(SocketErrorEventArgs e)
        {
            TelemetryClient?.TrackException(e.Exception);
            return Task.CompletedTask;
        }

        private static Task Client_ClientErrored(ClientErrorEventArgs e)
        {
            TelemetryClient?.TrackException(e.Exception);
            return Task.CompletedTask;
        }

        private static async Task ProcessMessage(DiscordMessage message, DiscordUser author, DiscordChannel channel)
        {
            DateTimeOffset startTime = DateTimeOffset.Now;
            if (!string.IsNullOrWhiteSpace(message?.Content) && !author.IsBot && !author.IsCurrent && !ProcessedMessageIds.Contains(message.Id))
            {
                if (message.Content.ToLower().StartsWith(Config.Prefix.ToLower()))
                {
                    _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Found command prefix, parsing...");
                    IEnumerable<string> commandSegments = Strings.SplitCommandLine(message.Content.Substring(Config.Prefix.Length));

                    foreach (IParseExtension extenstion in ParseExtensions)
                    {
                        commandSegments = extenstion.Parse(commandSegments, channel);
                    }

                    if (commandSegments.Any())
                    {
                        string commandAlias = commandSegments.First().ToLower();
                        IEnumerable<DiscordCommand> foundCommands = Commands.Where(c => c.Aliases.Any(a => a.ToLower() == commandAlias));
                        DiscordCommand commandToRun = foundCommands.FirstOrDefault();
                        if (foundCommands.Count() == 1)
                        {
                            await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, commandToRun, startTime);
                        }
                        else if (commandSegments.Count() >= 2)
                        {
                            foundCommands = AssemblyCommands.FirstOrDefault(c => c.Key.Name.ToLowerInvariant() == commandAlias)
                                .Value?.Where(c => c.Aliases.Contains(commandSegments.ElementAt(1).ToLower()));

                            if (foundCommands != null && foundCommands.Count() == 1)
                            {
                                await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(2), commandAlias, foundCommands.First(), startTime);
                            }
                            else
                            {
                                if (commandToRun != null)
                                {
                                    await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, foundCommands.First(), startTime);
                                }
                                else
                                {
                                    _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Unable to find command with alias \"{commandAlias}\".");
                                    await SendTemporaryMessage(message, author, channel, $"```\r\n{commandAlias}: command not found!\r\n```");
                                    TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, null, startTime, "404", false));
                                }
                            }
                        }
                        else if (commandToRun != null)
                        {
                            await ExecuteCommandAsync(message, author, channel, commandSegments.Skip(1), commandAlias, foundCommands.First(), startTime);
                        }
                    }
                }
            }
        }

        private static async Task ExecuteCommandAsync(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Found {command.Name} command!");
            ProcessedMessageIds.Add(message.Id);
            if (command.ArgumentCountPrecidate(commandSegments.Count()))
            {
                if (CheckPermissions(channel.Guild?.CurrentMember ?? Client.CurrentUser, channel, command))
                {
                    if (CheckPermissions(author, channel, command))
                    {
                        _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Running command \"{command.Name}\" asynchronously.");
                        new Task(async () => await RunCommandAsync(message, author, channel, commandSegments, commandAlias, command, start)).Start();
                    }
                    else
                    {
                        _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Attempt to run command without correct user permissions.");
                        await SendTemporaryMessage(message, author, channel, $"Oi! You're not alowed to run that command! Fuck off!");
                        TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, command, start, "401", false));
                    }
                }
                else
                {
                    _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] Attempt to run command without correct bot permissions.");
                    await SendTemporaryMessage(message, author, channel, $"Sorry! I don't have permission to run that command in this server! Contact an admin/mod for more info.");
                    TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, command, start, "403", false));
                }
            }
            else
            {
                await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
            }
        }

        private static async Task HandleBadRequest(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] {command.Name} does not take {commandSegments.Count()} arguments.");
            TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, command, start, "400", false));

            if (command.Usage != null)
            {
                await SendTemporaryMessage(message, author, channel, $"```\r\n{commandAlias} usage: {Config.Prefix}{commandAlias} [{command.Usage}]\r\n```");
            }
        }

        private static RequestTelemetry GetRequestTelemetry(DiscordUser author, DiscordChannel channel, DiscordCommand command, DateTimeOffset start, string code, bool success)
        {
            RequestTelemetry tel = new RequestTelemetry(command?.GetType().Name ?? "N/A", start, DateTimeOffset.Now - start, code, success);
            tel.Properties.Add("invoker", author.Id.ToString());
            tel.Properties.Add("channel", channel.Id.ToString());
            tel.Properties.Add("guild", channel.Guild?.Id.ToString());

            return tel;
        }

        private static async Task RunCommandAsync(DiscordMessage message, DiscordUser author, DiscordChannel channel, IEnumerable<string> commandSegments, string commandAlias, DiscordCommand command, DateTimeOffset start)
        {
            bool success = true;
            try
            {
                await channel.TriggerTypingAsync();
                command = InstantateDiscordCommand(command.GetType());
                HappinessData.TryGetValue(author.Id, out sbyte h);

                CommandContext context = new CommandContext(commandSegments.ToArray(), message, Client) { Happiness = h };
                CommandResult result = await command.RunCommand(commandSegments.ToArray(), context);

                _appLogArea.WriteLine($"[{message.Channel.Guild.Name ?? message.Channel.Name}] \"{command.Name}\" returned ReturnType.{result.ReturnType}.");
                if (result.ReturnType != ReturnType.None)
                {
                    if (result.ReturnType == ReturnType.Text && result.ResultText.Length > 2000)
                    {
                        for (int i = 0; i < result.ResultText.Length; i += 1993)
                        {
                            string str = result.ResultText.Substring(i, Math.Min(1993, result.ResultText.Length - i));
                            if (result.ResultText.StartsWith("```") && !str.StartsWith("```"))
                            {
                                str = "```" + str;
                            }
                            if (result.ResultText.EndsWith("```") && !str.EndsWith("```"))
                            {
                                str = str + "```";
                            }

                            _appLogArea.WriteLine($"Chunking message to {str.Length} chars");
                            await channel.SendMessageAsync(str);

                            await Task.Delay(2000);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(result.Attachment))
                        {
                            await channel.SendFileAsync(result.Attachment, result.ResultText, embed: result.ResultEmbed);
                        }
                        else if (result.Stream != null || result.ReturnType == ReturnType.File)
                        {
                            if (result.Stream.Length <= 8 * 1024 * 1024)
                            {
                                await channel.SendFileAsync(result.Stream, result.FileName, result.ResultText, false, result.ResultEmbed);
                            }
                            else
                            {
                                await channel.SendMessageAsync("This command has resulted in an attachment that is over 8MB in size and cannot be sent.");
                            }
                        }
                        else
                        {
                            await channel.SendMessageAsync(result.ResultText, embed: result.ResultEmbed);
                        }
                    }
                }

                RequestTelemetry request = GetRequestTelemetry(author, channel, command, start, success ? "200" : "500", success);
                foreach (var pair in result.InsightsData)
                {
                    request.Properties.Add(pair);
                }

                TelemetryClient?.TrackRequest(request);

                HappinessData[author.Id] = (sbyte)((int)(context.Happiness).Clamp(sbyte.MinValue, sbyte.MaxValue));
            }
            catch(BadArgumentsException)
            {
                await HandleBadRequest(message, author, channel, commandSegments, commandAlias, command, start);
            }
            catch (CommandException ex)
            {
                await channel.SendMessageAsync(ex.Message);
                TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (ArgumentOutOfRangeException)
            {
                await channel.SendMessageAsync("Hey there! That's gonna cause some issues, no thanks!!");
                TelemetryClient?.TrackRequest(GetRequestTelemetry(author, channel, command, start, "400", false));
            }
            catch (Exception ex)
            {
                success = false;
                ManageException(message, author, channel, ex, command);

                sbyte h = 0;
                HappinessData?.TryGetValue(author.Id, out h);

                HappinessData[author.Id] = (sbyte)((int)h - 1).Clamp(sbyte.MinValue, sbyte.MaxValue);
            }
            finally
            {
                if (command is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        private static async Task UpdateOwnerDm()
        {
            if (_ownerDm == null)
            {
                DiscordMember ownerMember = Client.Guilds.Values.SelectMany(g => g.Members).FirstOrDefault(m => m.Id == Client.CurrentApplication.Owner.Id);
                if (ownerMember != null)
                {
                    _appLogArea.WriteLine($"Owner member found! {ownerMember}");
                    _ownerDm = await ownerMember.CreateDmChannelAsync();
                }
                else
                {
                    _appLogArea.WriteLine("Unable to find owner member, Exception information will be unavailable.");
                }
            }
        }

        internal static bool CheckPermissions(DiscordUser author, DiscordChannel channel, DiscordCommand command)
        {
            bool go = true;

            if (command.HasAttribute<OwnerAttribute>())
            {
                if (author.Id == Client.CurrentApplication.Owner.Id || author.Id == Client.CurrentUser.Id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (command.HasAttribute<RequiresGuildAttribute>() && channel.IsPrivate)
            {
                return false;
            }

            if (author is DiscordMember memb)
            {
                Permissions p = memb.PermissionsIn(channel);
                go = p.HasFlag((memb.Id != Client.CurrentUser.Id ? command.UserPermissions : command.BotPermissions) ?? command.RequiredPermissions) || p.HasFlag(Permissions.Administrator);
            }
            else
            {
                go = true;
            }

            return go;
        }

        private static async Task SendWelcomeMessage(DiscordGuild g)
        {
            if (!Config.SeenGuilds.Contains(g.Id))
            {
                DiscordChannel channel = await GetFirstChannelAsync(g);

                if (channel != null)
                {
                    await channel.SendMessageAsync("Welcome to WamBot!");
                    await channel.SendMessageAsync("Thank you for adding WamBot to your server! Here's a few quick tips to get you started.");
                    await channel.SendMessageAsync($"Use `{Config.Prefix}help` for a list of available commands. To get info on when commands are added or removed, set an announements channel with `{Config.Prefix}announce`.");
                    await channel.SendMessageAsync($"For more information, visit http://wamwoowam.co.uk/projects/wambot/.");
                }

                Config.SeenGuilds.Add(g.Id);
            }
        }

        private static async Task<DiscordChannel> GetFirstChannelAsync(DiscordGuild g)
        {
            var channels = (await g.GetChannelsAsync())
                .Where(c => c.Type == ChannelType.Text)
                .OrderBy(c => c.Position);

            DiscordChannel channel =
                channels.FirstOrDefault(c => c.Name.ToLowerInvariant().Contains("general") && CanSendMessages(g, c)) ??
                channels.FirstOrDefault(c => CanSendMessages(g, c));
            return channel;
        }

        private static bool CanSendMessages(DiscordGuild g, DiscordChannel c)
        {
            return c.PermissionsFor(g.CurrentMember).HasPermission(Permissions.SendMessages);
        }

        private static async Task SendTemporaryMessage(DiscordMessage m, DiscordUser u, DiscordChannel c, string mess, int timeout = 5_000)
        {
            await Task.Yield();
            new Task(async () =>
            {
                DiscordMessage message = await c.SendMessageAsync(mess);
                await Task.Delay(timeout);
                await message.DeleteAsync();
            }).Start();
        }

        private static void ManageException(DiscordMessage message, DiscordUser author, DiscordChannel channel, Exception ex, DiscordCommand command)
        {
            _appLogArea.WriteLine($"\n --- Something's fucked up! --- \n{ex.ToString()}\n");
            TelemetryClient?.TrackException(ex, new Dictionary<string, string> { { "command", command.GetType().Name } });

            if (!(ex is TaskCanceledException) && !(ex is OperationCanceledException))
            {
                if (_ownerDm != null)
                {
                    try
                    {
                        _ownerDm.SendMessageAsync($"An {ex.GetType().Name} has occured processing command \"{command.Name}\" in {channel.Mention}{(channel.Guild != null ? $" ({channel.Guild.Name})" : "")}");
                        _ownerDm.SendMessageAsync($"```\r\n" +
                            $"{ex.ToString()}" +
                            $"```");
                        _ownerDm.SendMessageAsync($"Message content: `{message.Content}`");
                        _ownerDm.SendMessageAsync($"Message author:  {author.Mention}");
                    }
                    catch { }
                }

                new Task(async () =>
                {
                    try
                    {
                        DiscordMessage msg = await channel.SendMessageAsync(
                            $"Something's gone very wrong executing that command, and an {ex.GetType().Name} occured." +
                            $"{(_ownerDm != null ? "\r\nThis error has been reported, and should be fixed soon:tm:!" : "")}" +
                            $"\r\nThis message will be deleted in 10 seconds.");

                        await Task.Delay(10_000);
                        await msg.DeleteAsync();
                    }
                    catch { }
                }).Start();
            }
        }

        private static DiscordCommand InstantateDiscordCommand(Type type)
        {
            DiscordCommand command = null;
            IEnumerable<CommandAttribute> attributes = type.GetCustomAttributes<CommandAttribute>(true);

            if (!attributes.Any())
            {
                command = (DiscordCommand)Activator.CreateInstance(type);
            }
            else
            {
                List<object> commandParameters = new List<object>();

                if (attributes.Any(a => a.GetType() == typeof(HttpClientAttribute)))
                {
                    commandParameters.Add(_httpClient);
                }

                command = (DiscordCommand)Activator.CreateInstance(type, commandParameters.ToArray());
            }

            return command;
        }

    }
}
