using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamBot.Api;

namespace WhatTheActualFuckCommands.DotNetEval
{
    [Owner]
    class CSharpEvalCommand : DiscordCommand
    {
        public override string Name => "C# Eval";

        public override string Description => "Evaluates C# code";

        public override string[] Aliases => new[] { "cs" };

        public CommandResult Run(params string[] rawcode)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string code = Context.Message.Content
                .Substring(Context.Message.Content.IndexOf(" "))
                .Trim()
                .Trim('`');

            DiscordEmbedBuilder builder = Context.GetEmbedBuilder("Evaluation Result");
            builder.AddField("Code", $"```cs\r\n{Static.ShrinkToEmbed(code)}\r\n```");

            //Evidence evidence = new Evidence();
            //evidence.AddHostEvidence(new Zone(SecurityZone.Internet));
            //PermissionSet perms = SecurityManager.GetStandardSandbox(evidence);

            //StrongName[] names = new StrongName[1];
            //names[0] = Assembly.GetExecutingAssembly().Evidence.GetHostEvidence<StrongName>();

            //AppDomain domain = AppDomain.CreateDomain(".NET Eval", evidence, new AppDomainSetup(), perms, names);

            Static.RunEvalProcess(Context.Message, code, "cs", builder, (a, obj) =>
            {
                builder.AddField("Result", obj.ToString());

            });

            return builder.Build();
        }
    }
}
