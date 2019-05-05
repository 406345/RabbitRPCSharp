using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp
{
    public class RabbitMQConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string VirtualHost { get; set; } = "/rpc";

    }
}
