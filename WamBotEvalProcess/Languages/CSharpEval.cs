using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Newtonsoft.Json;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace WamBotEval.Languages
{
    class CSharpEval
    {
        static string template = @"
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace {0}
{{
    public class Script : MarshalByRefObject
    {{
        public object Main()
        {{
            {1}
            return null;
        }}
    }}
}}";

        internal static void RunEval(string code, PipeStream pipeStream, StreamWriter writer)
        {
            Evidence ev = new Evidence();
            ev.AddHostEvidence(new Zone(SecurityZone.Internet));
            PermissionSet pset = SecurityManager.GetStandardSandbox(ev);
            pset.AddPermission(new FileIOPermission(PermissionState.Unrestricted));

            AppDomainSetup ads = new AppDomainSetup
            {
                ApplicationBase = Path.Combine(Directory.GetCurrentDirectory(), "Sandbox")
            };

            AppDomain sandbox = AppDomain.CreateDomain("Sandbox", ev, ads, pset, null);

            string ns = "Eval_" + Guid.NewGuid().ToString().Replace("-", "_");
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults results = provider.CompileAssemblyFromSource(
                new CompilerParameters(new[] { "mscorlib.dll", "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Net.Http.dll" }),
                string.Format(template, ns, code));
            
            if (results.Errors.HasErrors)
            {
                writer.WriteLine(JsonConvert.SerializeObject(results.Errors.OfType<CompilerError>()));
            }
            else
            {
                dynamic obj = sandbox.CreateInstanceFromAndUnwrap(results.PathToAssembly, $"{ns}.Script");
                writer.WriteLine(JsonConvert.SerializeObject(obj.Main()));
            }
        }
    }
}
