using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WamBotRewrite.Api;

namespace WamBotRewrite.Commands
{
    [RunOutOfProcess]
    class CryptoCommands : CommandCategory
    {
        const string re = "That's a few too many repititions now don't'cha think?!";
        const int mr = int.MaxValue / 4;

        private static Encoding Utf8 = Encoding.UTF8;
        private static Lazy<MD5> _md5 = new Lazy<MD5>(() => MD5.Create());
        private static Lazy<SHA1> _sha1 = new Lazy<SHA1>(() => SHA1.Create());
        private static Lazy<SHA256> _sha256 = new Lazy<SHA256>(() => SHA256.Create());
        private static Lazy<SHA384> _sha384 = new Lazy<SHA384>(() => SHA384.Create());
        private static Lazy<SHA512> _sha512 = new Lazy<SHA512>(() => SHA512.Create());

        public override string Name => "Crypto";

        public override string Description => "Makes me do crypto gubbins!";

        [Command("GUID", "Generates a new GUID/UUID.", new[] { "guid", "uuid" })]
        public async Task GetGuid(CommandContext ctx)
        {
            await ctx.ReplyAsync(Guid.NewGuid().ToString());
        }

        [Command("MD5", "Generates an MD5 hash of a string.", new[] { "md5" })]
        public async Task GetMD5(CommandContext ctx, string str, [Range(0, mr, ErrorMessage = re)]int repetitions = 1)
        {
            await RunCryptoCommand(ctx, "MD5", _md5.Value, str, repetitions);
        }

        [Command("SHA-1", "Generates an SHA-1 hash of a string.", new[] { "sha1" })]
        public async Task GetSHA1(CommandContext ctx, string str, [Range(0, mr, ErrorMessage = re)]int repetitions = 1)
        {
            await RunCryptoCommand(ctx, "SHA-1", _sha1.Value, str, repetitions);
        }

        [Command("SHA-256", "Generates an SHA-256 hash of a string.", new[] { "sha2", "sha256" })]
        public async Task GetSHA256(CommandContext ctx, string str, [Range(0, mr, ErrorMessage = re)]int repetitions = 1)
        {
            await RunCryptoCommand(ctx, "SHA-256", _sha256.Value, str, repetitions);
        }

        [Command("SHA-384", "Generates an SHA-384 hash of a string.", new[] { "sha364" })]
        public async Task GetSHA384(CommandContext ctx, string str, [Range(0, mr, ErrorMessage = re)]int repetitions = 1)
        {
            await RunCryptoCommand(ctx, "SHA-384", _sha384.Value, str, repetitions);
        }

        [Command("SHA-512", "Generates an SHA-512 hash of a string.", new[] { "sha512" })]
        public async Task GetSHA512(CommandContext ctx, string str, [Range(0, mr, ErrorMessage = re)]int repetitions = 1)
        {
            await RunCryptoCommand(ctx, "SHA-512", _sha512.Value, str, repetitions);
        }

        [Command("AES Encrypt", "Encrypts a string using AES with a specified key.", new[] { "aesenc", "aes-encrypt" })]
        public async Task AESEncrypt(CommandContext ctx, string str, byte[] key = null)
        {
            var stopwatch = Stopwatch.StartNew();
            using (var aes = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                var builder = ctx.GetEmbedBuilder("AES");
                builder.AddField("Input String", $"```{str}```");

                try
                {
                    if (key == null)
                    {
                        aes.GenerateKey();
                        key = aes.Key;
                    }
                    else
                    {
                        aes.Key = key;
                    }

                    builder.AddField("Key", $"```{Convert.ToBase64String(aes.Key)}```");
                    aes.IV = Convert.FromBase64String("93/pCmMpbtCBycd6jZlppA==");

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(Encoding.UTF8.GetBytes(str), 0, Encoding.UTF8.GetByteCount(str));
                                cryptoStream.FlushFinalBlock();
                            }

                            builder.AddField("Encrypted Output", $"```{Convert.ToBase64String(memoryStream.ToArray())}```");
                        }
                    }
                }
                catch (CryptographicException ex)
                {
                    builder.AddField("Crypto Error", $"```{ex.Message}```");
                }

                builder
                    .WithFooter($"Encrypted in {stopwatch.ElapsedMilliseconds}ms")
                    .WithCurrentTimestamp();
                await ctx.ReplyAsync(string.Empty, emb: builder.Build());
            }
        }

        [Command("AES Decrypt", "Decrypts a string using AES with a specified key.", new[] { "aesdec", "aes-decrypt" })]
        public async Task AESDecrypt(CommandContext ctx, byte[] data, byte[] key)
        {
            var stopwatch = Stopwatch.StartNew();
            using (var aes = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            {
                var builder = ctx.GetEmbedBuilder("AES");
                builder.AddField("Input Data", $"```{Convert.ToBase64String(data)}```");
                builder.AddField("Key", $"```{Convert.ToBase64String(key)}```");
                try
                {
                    aes.Key = key;
                    aes.IV = Convert.FromBase64String("93/pCmMpbtCBycd6jZlppA==");

                    using (var decryptor = aes.CreateDecryptor(key, aes.IV))
                    {
                        using (var sourceStream = new MemoryStream(data))
                        {
                            sourceStream.Seek(0, SeekOrigin.Begin);
                            using (var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
                            {
                                using (var destinationStream = new MemoryStream())
                                {
                                    cryptoStream.CopyTo(destinationStream);
                                    builder.AddField("Decrypted Output", $"```{Encoding.UTF8.GetString(destinationStream.ToArray())}```");
                                }
                            }
                        }
                    }
                }
                catch (CryptographicException ex)
                {
                    builder.AddField("Crypto Error", $"```{ex.Message}```");
                }

                builder
                    .WithFooter($"Encrypted in {stopwatch.ElapsedMilliseconds}ms")
                    .WithCurrentTimestamp();
                await ctx.ReplyAsync(string.Empty, emb: builder.Build());
            }
        }

        private static async Task RunCryptoCommand(CommandContext ctx, string name, HashAlgorithm algorithm, string str, int repetitions)
        {
            var builder = ctx.GetEmbedBuilder(name);
            repetitions = Math.Abs(repetitions);
            byte[] b = Encoding.UTF8.GetBytes(str);

            if (repetitions < mr)
            {
                var message = await ctx.Channel.SendMessageAsync("Processing...");

                builder.AddField("Input String", $"```{str}```");
                var stopwatch = Stopwatch.StartNew();
                TimeSpan timeSpan;

                for (int i = 0; i < repetitions; i++)
                {
                    b = algorithm.ComputeHash(b);
                    if (repetitions > 2_000_000 && (i % 2_000_000) == 0)
                    {
                        timeSpan = TimeSpan.FromTicks((stopwatch.Elapsed.Ticks / (i + 1)) * (repetitions - i));
                        await message.ModifyAsync(p => p.Content = $"Processing... ({timeSpan:hh\\:mm\\:ss} remaining)");
                    }
                }

                builder.AddField("Output Hash (Base64)", $"```{Convert.ToBase64String(b)}```");
                builder.AddField("Output Hash (Hex)", $"```{string.Join("", b.Select(by => by.ToString("x2")))}```");
                builder
                    .WithFooter($"Computed {repetitions} repitition(s) of hash in {stopwatch.ElapsedMilliseconds}ms")
                    .WithCurrentTimestamp();

                await message.DeleteAsync();
                await ctx.ReplyAsync(string.Empty, emb: builder.Build());
            }
            else
            {
                await ctx.ReplyAsync(re);
            }
        }
    }
}
