using Markdig;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    [RequiresGuild]
    class EmailCommands : CommandCategory
    {
        public override string Name => "Email";

        public override string Description => "Allows you to spend WamCash sending emails";

        private static MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UseBootstrap()
            .DisableHtml()
            .Build();

        [Command("Send", "Sends an email.", new[] { "send", "mail", "email" })]
        public async Task Send(CommandContext ctx, [EmailAddress] string address, string subject, params string[] message)
        {
            if (Program.Config.Email.Enabled)
            {
                using (SmtpClient client = new SmtpClient("localhost"))
                {
                    client.Credentials = new NetworkCredential(Program.Config.Email.Username, Program.Config.Email.Password);

                    var botAddress = new MailAddress(Program.Config.Email.Username, $"{ctx.Author} via WamBot");
                    try
                    {
                        var toAddress = new MailAddress(address);
                        try
                        {
                            var m = new MailMessage(botAddress, toAddress)
                            {
                                Subject = subject,
                                Body = $"{Markdown.ToHtml(string.Join(" ", message).Replace("\\\"", "\""), pipeline)}<hr />" +
                                $"<p>This is an email from {ctx.Author} sent via WamBot.To report abuse, contact {Program.Application.Owner} on Discord" +
                                $"<br />or forward this email to <a href=\"mailto:{Program.Config.Email.AbuseEmail}\">{Program.Config.Email.AbuseEmail}</a></p> ",
                                IsBodyHtml = true
                            };

                            await ctx.EnsureBallanceAsync(50 + 100 * ctx.Message.Attachments.Count, "Email");

                            foreach (var a in ctx.Message.Attachments)
                            {
                                m.Attachments.Add(new Attachment(await HttpClient.GetStreamAsync(a.ProxyUrl), a.Filename));
                            }

                            await client.SendMailAsync(m);
                            await ctx.ReplyAsync("Email sent!");
                        }
                        catch (CommandException e)
                        {
                            await ctx.ReplyAsync(e.Message);
                        }
                        catch (Exception ex)
                        {
                            Tools.ManageException(ctx.Message, ctx.Channel, ex, ctx.Command);
                        }
                    }
                    catch
                    {
                        await ctx.ReplyAsync("Hey! That email address doesn’t look right! Check it out and try again.");
                    }
                }
            }
            else
            {
                await ctx.ReplyAsync("Sorry! This feature is currently disabled.");
            }
        }
    }
}
