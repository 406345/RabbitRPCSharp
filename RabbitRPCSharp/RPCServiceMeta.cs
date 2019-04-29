using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp
{
    internal class RPCServiceMeta
    {
        public Type ServiceType { get; set; }
        public Attributes.RPCService Service { get; set; }
        public object ServiceInstance { get; set; } = null;

        public List<RPCMethodMeta> Methods { get; set; } = new List<RPCMethodMeta>();
    }
}
