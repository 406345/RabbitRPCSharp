using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp.Attributes
{
    /// <summary>
    /// Mark the function will call the remoting function
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RabbitRPCSharpCallerMethod : Attribute
    {
        public RabbitRPCSharpCallerMethod()
        {
        }

        public string Name { get; set; }
    }
}
