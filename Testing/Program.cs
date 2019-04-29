using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitRPCSharp;
using RabbitRPCSharp.Attributes;
using System;
using System.Threading;

namespace Testing
{
    class Program
    { 
        [RPCService]
        class TestService
        {
            [RPCMethod]
            public int Add(int t, int b)
            {
                return t * b;
            }

            [RPCMethod]
            public int Subtile(int t, int b)
            {
                return t / b;
            }
        }

        static void Main(string[] args)
        {
            new Thread(a => {
                var service = RabbitRPCSharp.RabbitRPCSharpService.Instance;
                service.RabbitMQHost        = "mq.gezhigene.com";
                service.RabbitMQUsername    = "rpc";
                service.RabbitMQPassword    = "yh123456";
                service.RabbitMQPost        = 15672;
                service.RabbitMQVirtualHost = "/rpc";
                service.Run();
            }).Start();

            MQService mq = new MQService();
            mq.RabbitMQHost = "mq.gezhigene.com";
            mq.RabbitMQUsername = "rpc";
            mq.RabbitMQPassword = "yh123456";
            mq.RabbitMQPost = 15672;
            mq.RabbitMQVirtualHost = "/rpc";

            foreach (var item in mq.Receive("rpcResult"))
            {

            }

            Console.ReadLine();
        }
    }
}
