﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Scripting.JavaScript
{
    public enum JavaScriptMemoryAllocationEventType
    {
        AllocationRequest,
        Free,
        Failure,
    }
}
