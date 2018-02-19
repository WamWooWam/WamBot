using System;
using System.Collections.Generic;
using System.Text;
using WamBot.Api;
using System.Threading.Tasks;
using Renci.SshNet;
using System.Security.Cryptography;
using Renci.SshNet.Common;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

namespace WhatTheActualFuckCommands.SSH
{
    [RequiresGuild]
    class VistaSSHCommand : ModernDiscordCommand
    {
        static SshClient _adminClient = null;
        static Dictionary<ulong, Tuple<ShellStream, SshClient, CancellationTokenSource>> _clientDictionary = new Dictionary<ulong, Tuple<ShellStream, SshClient, CancellationTokenSource>>();


        public override string Name => "Windows Vista SSH";

        public override string Description => "Allows remote SSH access to a Windows Vista virtual machine";

        public override string[] Aliases => new[] { "vista", "ssh" };

        public CommandResult Cancel()
        {
            if (_clientDictionary.TryGetValue(Context.Guild.Id, out var client))
            {
                client.Item3.Cancel();
                return "Cancelled!";
            }
            else
            {
                return "Nothing to cancel!";
            }
        }

        public CommandResult Run(params string[] args)
        {
            if (_adminClient == null)
            {
                _adminClient = new SshClient(this.GetData<string>("sshhost"), "admin", this.GetData<string>("adminpass"));
                _adminClient.Connect();
            }

            string command = string.Join(" ", args.Select(a => a.Any(c => char.IsWhiteSpace(c)) ? $@"""{a}""" : a));

            if (_clientDictionary.TryGetValue(Context.Guild.Id, out var client))
            {

                try
                {
                    ShellStream str = null;
                    if (!client.Item2.IsConnected || client.Item3.IsCancellationRequested)
                    {
                        str = FixStream(client);
                    }
                    else
                    {
                        str = client.Item1;
                    }

                    return FullRunCommand(command, client, str);
                }
                catch (ObjectDisposedException)
                {
                    ShellStream str = FixStream(client);
                    return FullRunCommand(command, client, str);
                }
            }
            else
            {
                SshClient newClient = new SshClient(
                    (this).GetData<string>("sshhost"),
                    Context.Guild.Id.ToString(),
                    GetGuildAccountPassword());

                SSHConnnect(newClient);

                CancellationTokenSource source = new CancellationTokenSource();
                var stream = newClient.CreateShellStream("cmd", 35, 80, 35 * 12, 80 * 8, 35 * 80 * sizeof(char));

                var c = new Tuple<ShellStream, SshClient, CancellationTokenSource>(stream, newClient, source);
                _clientDictionary[Context.Guild.Id] = c;
                return FullRunCommand(command, c, stream);
            }
        }

        private CommandResult FullRunCommand(string command, Tuple<ShellStream, SshClient, CancellationTokenSource> client, ShellStream str)
        {
            CancellationTokenSource src = client.Item3;
            CommandResult ret = RunCommand(command, str, ref src);
            _clientDictionary[Context.Guild.Id] = new Tuple<ShellStream, SshClient, CancellationTokenSource>(str, client.Item2, src);
            return Regex.Replace(ret.ResultText , @"\e\[(\d+;)*(\d+)?[ABCDHJKfmsu]", ""); ;
        }

        private ShellStream FixStream(Tuple<ShellStream, SshClient, CancellationTokenSource> client)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            ShellStream str;
            SSHConnnect(client.Item2);
            str = client.Item2.CreateShellStream("cmd", 35, 80, 35 * 12, 80 * 8, 35 * 80 * sizeof(char));
            _clientDictionary[Context.Guild.Id] = new Tuple<ShellStream, SshClient, CancellationTokenSource>(str, client.Item2, source);
            return str;
        }

        private void SSHConnnect(SshClient newClient)
        {
            if (newClient.IsConnected)
            {
                return;
            }

            try
            {
                newClient.Connect();
            }
            catch (SshAuthenticationException)
            {
                _adminClient.RunCommand($"net user {Context.Guild.Id} {GetGuildAccountPassword()} /add");
                newClient.Connect();
            }
        }

        private static CommandResult RunCommand(string cmd, ShellStream stream, ref CancellationTokenSource source)
        {
            stream.WriteLine(cmd);
            StringBuilder ret = new StringBuilder();
            while (true)
            {
                try
                {
                    source.Token.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                    string str = stream.ReadLine(TimeSpan.FromSeconds(1));
                    if (str != null)
                    {
                        if (str.Trim() != cmd)
                        {
                            ret.Append(str.TrimEnd() + "\r\n");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    source = new CancellationTokenSource();
                    break;
                }
            }

            return $"```{ret}```";
        }

        private void Stream_DataReceived(object sender, ShellDataEventArgs e)
        {

        }

        private string GetGuildAccountPassword()
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(Context.Guild.Id.ToString()))).Substring(0, 14);
            }
        }
    }
}
