using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WamBotRewrite.Api
{
    [Serializable]
    class CommandException : Exception, ISerializable
    {
        [JsonConstructor]
        private CommandException() { }

        public CommandException(string message) : base(message) { }

        public CommandException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
}
