using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Api
{
    public interface ICommandsAssembly
    {
        string Name { get; }

        string Description { get; }
    }
}
