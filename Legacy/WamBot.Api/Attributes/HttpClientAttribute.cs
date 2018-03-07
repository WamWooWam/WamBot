using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Api
{
    /// <summary>
    /// Attribute used to provide a command a <see cref="System.Net.Http.HttpClient"/>, requires a constructor that accepts said client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class HttpClientAttribute : BaseCommandAttribute
    {        
        public HttpClientAttribute()
        {
        }

        public bool Required { get; set; } = true;
    }
}
