using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp.Attributes
{
    /// <summary>
    /// Mark the function will call the remoting function
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false , Inherited = true)]
    public class RabbitRPCShaprCaller : Attribute
    {
        public  RabbitRPCShaprCaller()
        {
        }

        public string Name { get; set; }
    }
}
