using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using RabbitMQ.Client;
using System.IO;
using Newtonsoft.Json.Bson;

namespace RabbitRPCSharp
{
    public class RabbitRPCSharpClient : IDisposable
    {
        public string Id { get; private set; }
        public RabbitMQConfig MQConfig { get; set; } = new RabbitMQConfig();
        private string QueueName { get; set; }
        
        private IBasicProperties property;

        MQService mq = new MQService();

        public RabbitRPCSharpClient()
        {
            this.Id = Guid.NewGuid().ToString();
            this.MQConfig.Host = "mq.gezhigene.com";
            this.MQConfig.Username = "rpc";
            this.MQConfig.Password = "yh123456";
            this.MQConfig.Port = 15672;
            this.MQConfig.VirtualHost = "/rpc";
            mq.MQConfig = this.MQConfig;
            mq.Connect();
            property = mq.Channel.CreateBasicProperties();
            property.CorrelationId = this.Id;
        }

        public void Dispose()
        {
            this.mq.Disconnect();
            this.mq.Dispose();
            this.mq = null;
        }

        public T Call<T>(string service,string method,params object[] args)
        {
            //if(!string.IsNullOrEmpty(this.QueueName))mq.Channel.QueueDelete(this.QueueName, false, false);
            this.QueueName = mq.Channel.QueueDeclare("", false, false, true, null).QueueName;
            property.ReplyTo = this.QueueName;

            Dictionary<string, object> dict = new Dictionary<string, object>();
            int index = 0;
            foreach (var item in args)
            {
                dict.Add(index.ToString(), item);
                ++index;
            }

            mq.Send(service, method, SerializeData(dict), property);
            //mq.Send(service, method, dict, property);

            foreach (var item in mq.Receive(this.QueueName))
            {
                if (item.BasicProperties.CorrelationId != this.Id)
                    continue;

                var result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(item.Body));
                return result;
            }

            return default(T);
        }

        public object Call(string service, string method, params object[] args)
        {

            return this.Call<object>(service,method,args);
        }

        protected virtual byte[] SerializeData(object data)
        {
            MemoryStream ms = new MemoryStream();

            using (BsonWriter writer = new BsonWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, data);
            }

            return ms.ToArray();
        }
    }
}
