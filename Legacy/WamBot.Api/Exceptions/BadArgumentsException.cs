using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WamBot.Api
{
    [Serializable]
    internal class BadArgumentsException : Exception
    {
        public BadArgumentsException() { }
        public BadArgumentsException(string message) : base(message) { }
        public BadArgumentsException(string message, Exception inner) : base(message, inner) { }
        protected BadArgumentsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
