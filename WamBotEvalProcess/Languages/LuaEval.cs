using Newtonsoft.Json;
using NLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBotEval.Languages
{
    class LuaEval
    {
        internal static void RunEval(string code, PipeStream pipeStream, StreamWriter writer)
        {
            using (Lua lua = new Lua())
            {
                try
                {
                    lua.DoString(@"os.execute = nil; os.rename = nil; os.remove = nil; io = nil; import = nil;");
                    var ret = lua.DoString(code, "eval")?
                        .Select(o => o is LuaTable ta ? TableToString(ta) : o); // bodge for tables

                    writer.WriteLine(JsonConvert.SerializeObject(ret));
                }
                catch (Exception ex)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(ex));
                }
            }
        }

        private static string TableToString(LuaTable t)
        {
            object[] keys = new object[t.Keys.Count];
            object[] values = new object[t.Values.Count];
            t.Keys.CopyTo(keys, 0);
            t.Values.CopyTo(values, 0);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < keys.Count(); i++)
            {
                builder.AppendLine($"[{keys[i]}, {values[i]}]");
            }

            return builder.ToString();
        }
    }
}
