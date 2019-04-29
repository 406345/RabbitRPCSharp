using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp.Attributes
{
    /// <summary>
    /// Mark the function that will be called remotely
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RPCMethod : Attribute
    {
        public RPCMethod()
        {
        }

        public string Name { get; set; }
    }
}
