using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp
{
    class RPCMessage
    {
        public Dictionary<string,object> args { get; set; } = new Dictionary<string, object>();
    }
}
