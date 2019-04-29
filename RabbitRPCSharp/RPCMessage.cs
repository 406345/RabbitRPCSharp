using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp
{
    class RPCMessage
    {
        public string message_id{ get; set; }
        public Dictionary<string,object> args { get; set; } = new Dictionary<string, object>();
    }
}
