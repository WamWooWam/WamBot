using System;

namespace WamBot.Api
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class CommandAttribute : Attribute
    {
    }
}