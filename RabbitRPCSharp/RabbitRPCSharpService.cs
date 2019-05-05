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
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Bson;

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

        public RabbitMQConfig MQConfig { get; set; } = new RabbitMQConfig();
       
        private MQService mqService;
        private Assembly hostAssembly = null;
        private List<RPCServiceMeta> metas = new List<RPCServiceMeta>();
        private BlockingCollection<Tuple<BasicDeliverEventArgs, IModel>> messagePool = new BlockingCollection<Tuple<BasicDeliverEventArgs, IModel>>();

        protected RabbitRPCSharpService(Assembly host)
        {
            this.hostAssembly = host;

            var types = this.hostAssembly.GetTypes();

            foreach (var type in types)
            {
                var rpcService = type.GetCustomAttribute<RabbitRPCSharp.Attributes.RPCService>();

                if (rpcService == null)
                    continue;

                if (type.IsAbstract)
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
        }

        public void Run()
        {
            mqService = new MQService();
            mqService.MQConfig.Host = "mq.gezhigene.com";
            mqService.MQConfig.Username = "rpc";
            mqService.MQConfig.Password = "yh123456";
            mqService.MQConfig.Port = 15672;
            mqService.MQConfig.VirtualHost = "/rpc";
            mqService.Connect();
            mqService.Channel.BasicQos(0, 1, false);
            mqService.OnMessage += MqService_OnMessage;

            foreach (var item in this.metas)
            {
                var servicename = item.ServiceType.Name;// string.isnullorempty(item.service.name) ? item.servicetype.name : item.service.name;
                mqService.Channel.QueueDeclare(servicename, false, false, false, null);
                mqService.Channel.ExchangeDeclare(servicename, "direct", false, false, null);

                foreach (var method in item.Methods)
                {
                    var methodname = method.MethodType.Name;// string.isnullorempty(method.method.name) ? method.methodtype.name : method.method.name;
                    mqService.Channel.QueueBind(servicename, servicename, methodname, null);
                }

                mqService.ReceiveAsync(servicename);
            }

           

            while (true)
            {
                var args = this.messagePool.Take();
                this.HandleMessage(args.Item1,args.Item2);
            }
            //foreach (var item in mqService.Receive("rpc"))
            //{
            //    this.HandleMessage(item);
            //}
        }

        private void MqService_OnMessage(BasicDeliverEventArgs arg1, IModel arg2)
        {
            this.messagePool.Add(new Tuple<BasicDeliverEventArgs, IModel>(arg1,arg2));
        }

        

        private void HandleMessage(BasicDeliverEventArgs args,IModel model)
        {
            var serviceName = args.Exchange;
            var methodName = args.RoutingKey;
            var meta = this.metas.Where(x => x.ServiceType.Name == serviceName).FirstOrDefault();

            if (meta == null)
                return;

            var method = meta.Methods.Where(x => x.MethodType.Name == methodName).FirstOrDefault();

            if (method == null)
                return;

            try
            {

                var msg = DeserializeData<Dictionary<string,object>>(args.Body);
                //var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(args.Body));
                var ps = method.MethodType.GetParameters();

                List<object> arrays = new List<object>();

                for (int i = 0; i < ps.Length; i++)
                {
                    var v = (msg.Values.ToArray()[i]);
                    arrays.Add(Convert.ChangeType(v, ps[i].ParameterType));
                }

                var ret = method.MethodType.Invoke(meta.ServiceInstance, arrays.ToArray());

                var callerId = args.BasicProperties.CorrelationId;
                var replyTo = args.BasicProperties.ReplyTo;

                var property = model.CreateBasicProperties();
                property.CorrelationId = callerId;
                model.BasicPublish(exchange: "", routingKey: replyTo,
                          basicProperties: property, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ret)));

                property = null;

            }
            finally
            {

            }
        }
         
        protected virtual T DeserializeData<T>(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            using (BsonReader reader = new BsonReader(ms))
            {
                JsonSerializer serializer = new JsonSerializer();

                T e = serializer.Deserialize<T>(reader);

                return e;
            }
        }
    }
}
