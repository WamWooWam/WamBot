﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Scripting.JavaScript.SafeHandles
{
    internal sealed class JavaScriptRuntimeSafeHandle : SafeHandle
    {
        public JavaScriptRuntimeSafeHandle():
            base(IntPtr.Zero, ownsHandle: true)
        {

        }

        public JavaScriptRuntimeSafeHandle(IntPtr handle):
            base(handle, true)
        {

        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }

        protected override bool ReleaseHandle()
        {
            if (IsInvalid)
                return false;

            var toRelease = this.handle;

            var error = ChakraApi.Instance.JsReleaseCurrentContext();
            Debug.Assert(error == JsErrorCode.JsNoError);

            error = ChakraApi.Instance.JsDisposeRuntime(toRelease);
            Debug.Assert(error == JsErrorCode.JsNoError);
            return true;
        }
    }
}
