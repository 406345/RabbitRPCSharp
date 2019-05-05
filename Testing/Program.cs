using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitRPCSharp;
using RabbitRPCSharp.Attributes;
using System;
using System.Diagnostics;
using System.Threading;

namespace Testing
{
    [RPCService]
    static class RemoteMethod
    {
        [RPCMethod]
        public static void T(this RabbitRPCSharpClient client)
        {

        }
    }

    class Program
    { 
        [RPCService]
        class TestService
        {
            [RPCMethod]
            public int Add(int t, int b)
            {
                return t + b;
            }

            [RPCMethod]
            public int Subtile(int t, int b)
            {
                return t-b;
            }
        }

        [RPCService]
        class TestService2
        {
            [RPCMethod]
            public int Add(int t, int b)
            {
                return t + b;
            }

            [RPCMethod]
            public int Subtile(int t, int b)
            {
                return t - b;
            }

            [RPCMethod]
            public byte[] byteTest(byte[] buffer)
            {
                return buffer;
            }
        }
        static void Client()
        {
            Random rnd = new Random();
            int total = 0;
            for (int i = 0; i < 20; i++)
            {
                new Thread(a =>
                {
                    RabbitRPCSharp.RabbitRPCSharpClient ff = new RabbitRPCSharpClient();
                    ff.MQConfig.Host = "mq.gezhigene.com";
                    ff.MQConfig.Username = "rpc";
                    ff.MQConfig.Password = "yh123456";
                    ff.MQConfig.Port = 15672;
                    ff.MQConfig.VirtualHost = "/rpc";

                    while (true)
                    {
                        int va = rnd.Next(1, 200), vb = rnd.Next(200, 400);
                        

                        byte[] buffer = new byte[1024];
                        rnd.NextBytes(buffer);
                        System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start(); //  开始监视代码运行时间

                        var ret = ff.Call<byte[]>("TestService2", "byteTest", buffer);

                        stopwatch.Stop();


                        int v = Interlocked.Add(ref total, 1);
                        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds}][{v}][{a}] callback value = {ret.Length} real = { buffer.Length} equal = { ret[0] == buffer[0] }");
                        //Console.WriteLine($"[{v}][{a}] callback value = {ret} real = { va + vb} equal = { (va + vb) == ret }");
                    }
                }).Start(i);
            }

        }

        static void Server()
        {
            var service = RabbitRPCSharp.RabbitRPCSharpService.Instance;
            service.MQConfig.Host = "mq.gezhigene.com";
            service.MQConfig.Username = "rpc";
            service.MQConfig.Password = "yh123456";
            service.MQConfig.Port = 15672;
            service.MQConfig.VirtualHost = "/rpc";
            service.Run();
        }
        static void Main(string[] args)
        { 
#if DEBUG
            Server();
            Console.WriteLine("Server mode started");
#else
            Client();
            Console.WriteLine("Client mode started");
#endif
            Console.ReadLine();
        }
    }
}
