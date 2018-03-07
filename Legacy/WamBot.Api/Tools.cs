using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Api
{
    public static class Tools
    {
        internal static List<IParamConverter> ParameterParseHelpers = new List<IParamConverter>();

        public static DiscordEmbedBuilder GetEmbedBuilder(this CommandContext context, string title = null)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.WithAuthor($"{(title != null ? $"{title} - " : "")}WamBot 3.0.0", icon_url: context.Client.CurrentApplication.Icon);
            //embedBuilder.WithFooter($"Lovingly made by @{context.Client.CurrentApplication.Owner.Username}#{context.Client.CurrentApplication.Owner.Discriminator} using D#+. My current prefix is `{context.Prefix}`.", context.Client.CurrentApplication.Owner.AvatarUrl);
            embedBuilder.WithColor(context.Message.Author is DiscordMember m ? m.Color : new DiscordColor(0, 137, 255));
            return embedBuilder;
        }

        public static HappinessLevel GetHappinessLevel(sbyte h)
        {
            if (h >= 102)
            {
                return HappinessLevel.Adore;
            }

            if (h < 102 && h >= 51)
            {
                return HappinessLevel.Like;
            }

            if (h < 51 && h >= -51)
            {
                return HappinessLevel.Indifferent;
            }

            if (h < -51 && h >= -102)
            {
                return HappinessLevel.Dislike;
            }

            if (h < -102)
            {
                return HappinessLevel.Hate;
            }

            return HappinessLevel.Indifferent;
        }

        internal static bool IsImplicit(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ImplicitAttribute), false);
        }

        internal static bool IsParams(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ParamArrayAttribute), false);
        }

        internal static void RegisterPatameterParseExtension(IParamConverter parseExtension) => ParameterParseHelpers.Add(parseExtension);

        public static bool HasAttribute<T>(this object o) where T : Attribute
        {
            MemberInfo t = o.GetType();
            if (o is MemberInfo m)
            {
                t = m;
            }

            return t.GetCustomAttributes(true).Any(a => a is T);
        }

        public static T GetAttribute<T>(this object o) where T : Attribute
        {
            MemberInfo t = o.GetType();
            if (o is MemberInfo m)
            {
                t = m;
            }

            return t.GetCustomAttributes(true).FirstOrDefault(a => a is T) as T;
        }

        public static async Task<T> FindOrCreateAsync<T>(this DbSet<T> set, object key, Func<T> newPrecidate) where T : class
        {
            T t = await set.FindAsync(key);
            if (t == null)
            {
                t = newPrecidate();
                set.Add(t);
            }

            return t;
        }

    }

    public class MemberEquality : IEqualityComparer<DiscordMember>
    {
        public bool Equals(DiscordMember x, DiscordMember y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(DiscordMember obj)
        {
            return (int)obj.Id;
        }
    }
}
