using Microsoft.CSharp;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RabbitRPCSharp
{
    public class RabbitRPCSharpService
    {
        static RabbitRPCSharpService instance_ = null;
        public static RabbitRPCSharpService Instance
        {
            get
            {
                if (instance_ == null)
                    instance_ = new RabbitRPCSharpService(Assembly.GetCallingAssembly());

                return instance_;
            }
        }

        public string RabbitMQHost { get; set; } = "";
        public int RabbitMQPost { get; set; } = 5672;
        public string RabbitMQUsername { get; set; } = "";
        public string RabbitMQPassword { get; set; } = "";
        public string RabbitMQVirtualHost { get; set; } = "/rpc";

        private MQService mqService;
        private Assembly hostAssembly = null;
        private List<RPCServiceMeta> metas = new List<RPCServiceMeta>();

        protected RabbitRPCSharpService(Assembly host)
        {
            this.hostAssembly = host;

            var types = this.hostAssembly.GetTypes();

            foreach (var type in types)
            {
                var rpcService = type.GetCustomAttribute<RabbitRPCSharp.Attributes.RPCService>();

                if (rpcService == null)
                    continue;

                var meta = new RPCServiceMeta();
                meta.Service = rpcService;
                meta.ServiceType = type;
                meta.Service.Name = string.IsNullOrEmpty(rpcService.Name) ? type.Name : rpcService.Name;
                meta.ServiceInstance = type.Assembly.CreateInstance(type.FullName);

                foreach (var method in type.GetMethods())
                {
                    var rpcMethod = method.GetCustomAttribute<RabbitRPCSharp.Attributes.RPCMethod>();

                    if (rpcMethod== null)
                        continue;

                    var m = new RPCMethodMeta()
                    {
                        Method = rpcMethod,
                        MethodType = method
                    };

                    m.Method.Name = string.IsNullOrEmpty(rpcMethod.Name) ? method.Name : rpcMethod.Name;
                    meta.Methods.Add(m);


                }

                metas.Add(meta);
            }

            this.BindMeta(metas);
        }

        private void BindMeta(List<RPCServiceMeta> ms)
        {
             
        }

        public void Run()
        {
            mqService = new MQService();
            mqService.RabbitMQHost = "mq.gezhigene.com";
            mqService.RabbitMQUsername = "rpc";
            mqService.RabbitMQPassword = "yh123456";
            mqService.RabbitMQPost = 15672;
            mqService.RabbitMQVirtualHost = "/rpc";
            mqService.Connect();
            mqService.Channel.QueueDeclare("rpc", false, false, false, null);

            foreach (var item in this.metas)
            {
                var servicename = item.ServiceType.Name;// string.isnullorempty(item.service.name) ? item.servicetype.name : item.service.name;
                mqService.Channel.ExchangeDeclare(servicename, "direct", true, false, null);

                foreach (var method in item.Methods)
                {
                    var methodname = method.MethodType.Name;// string.isnullorempty(method.method.name) ? method.methodtype.name : method.method.name;
                    mqService.Channel.QueueBind("rpc", servicename, methodname, null);
                }
            }

            foreach (var item in mqService.Receive("rpc"))
            {
                this.HandleMessage(item);
            }
            //var factory = new RabbitMQ.Client.ConnectionFactory();
            //factory.UserName =this.RabbitMQUsername;
            //factory.Password = this.RabbitMQPassword;
            //factory.VirtualHost = this.RabbitMQVirtualHost;
            //factory.HostName = this.RabbitMQHost;
            //factory.Port = this.RabbitMQPost;

            //using (var connection = factory.CreateConnection())
            //{
            //    using (var channel = connection.CreateModel())
            //    {
            //        channel.QueueDeclare("rpc", false, false, false, null);

            //        foreach (var item in this.metas)
            //        {
            //            var serviceName = item.ServiceType.Name;// string.IsNullOrEmpty(item.Service.Name) ? item.ServiceType.Name : item.Service.Name;
            //            channel.ExchangeDeclare(serviceName, "direct", true, false, null);

            //            foreach (var method in item.Methods)
            //            {
            //                var methodName = method.MethodType.Name;// string.IsNullOrEmpty(method.Method.Name) ? method.MethodType.Name : method.Method.Name;
            //                channel.QueueBind("rpc", serviceName, methodName, null);
            //            }
            //        }

            //        //var c = new EventingBasicConsumer(channel);
            //        var consumer = new QueueingBasicConsumer(channel);
            //        channel.BasicConsume("rpc", true, consumer);

            //        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")}] Connected to MQ server");



            //        while (true)
            //        {
            //            var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
            //            //var body = ea.Body;
            //            //var message = Encoding.UTF8.GetString(body);
            //            this.HandleMessage(channel,ea);
            //        }
            //    }
            //}
        }


        private void HandleMessage(BasicDeliverEventArgs args)
        {
            var serviceName = args.Exchange;
            var methodName = args.RoutingKey;
            var meta = this.metas.Where(x => x.ServiceType.Name == serviceName).FirstOrDefault();

            if (meta == null)
                return;

            var method = meta.Methods.Where(x => x.MethodType.Name == methodName).FirstOrDefault();

            if (method == null)
                return;

            var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<RPCMessage>(Encoding.UTF8.GetString(args.Body));

            var ps = method.MethodType.GetParameters();

            List<object> arrays = new List<object>();

            for (int i = 0; i < ps.Length; i++)
            {
                var v = (msg.args.Values.ToArray()[i]);
                arrays.Add(Convert.ChangeType(v, ps[i].ParameterType));
            }

            var ret = method.MethodType.Invoke(meta.ServiceInstance, arrays.ToArray());

            mqService.Send("rpcResult", msg.message_id, ret);
            //channel.BasicPublish("result", msg.message_id, false, null, null);
            //Console.WriteLine(ret);
        }
    }
}
