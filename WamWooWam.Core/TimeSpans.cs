using System;
using System.Collections.Generic;
using System.Text;

namespace WamWooWam.Core
{
    public static class TimeSpans
    {
        public static string ToNaturalString(this TimeSpan time)
        {
            var builder = new StringBuilder();
            if (time.Days > 0)
            {
                builder.Append($"{time.Days} {(time.Days == 1 ? "day" : "days")} ");
            }

            if (time.Hours > 0)
            {
                builder.Append($"{time.Hours} {(time.Hours == 1 ? "hour" : "hours")} ");
            }

            if (time.Minutes > 0)
            {
                builder.Append($"{time.Minutes} {(time.Minutes == 1 ? "minute" : "minutes")} ");
            }

            if (time.Seconds > 0)
            {
                if (time.Minutes > 0 || time.Hours > 0)
                    builder.Append("and ");

                builder.Append($"{time.Seconds} {(time.Seconds == 1 ? "second" : "seconds")}");
            }

            return builder.ToString();
        }
    }
}
