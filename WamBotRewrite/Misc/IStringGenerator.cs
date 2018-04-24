using System;
using System.Collections.Generic;
using System.Text;
using WamBotRewrite.Data;

namespace WamBotRewrite
{
    interface IStringGenerator
    {
        string Generate(int length);

        string Generate(User user, int length);

        void Train(User user, string[] strings);

        void Reset(User user);
    }
}
