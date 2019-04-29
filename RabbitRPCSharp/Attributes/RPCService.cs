using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp.Attributes
{
    /// <summary>
    /// Mark the function that will be called remotely
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RPCService : Attribute
    {
        public RPCService()
        {
        }

        public string Name { get; set; }
    }
}
