using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;

namespace RabbitRPCSharp
{
    public class MQService : IDisposable
    {
        public RabbitMQConfig MQConfig { get; set; } = new RabbitMQConfig();
     
        public IConnection Connection { get; private set; }
        public IModel Channel { get; private set; }
        public DefaultBasicConsumer Consumer { get; private set; }
        public event Action<BasicDeliverEventArgs, IModel> OnMessage;
        BlockingCollection<BasicDeliverEventArgs> msgPool = new BlockingCollection<BasicDeliverEventArgs>();

        public void Dispose()
        {
            if( this.Channel !=null)
            {
                this.Channel.Close();
                this.Channel.Dispose();
            }

            if (this.Connection != null)
            {
                this.Connection.Close();
                this.Connection.Dispose();
            }
        }

        public void Connect()
        {
            var factory = new RabbitMQ.Client.ConnectionFactory();
            factory.UserName = this.MQConfig.Username;
            factory.Password = this.MQConfig.Password;
            factory.VirtualHost = this.MQConfig.VirtualHost;
            factory.HostName = this.MQConfig.Host;
            factory.Port = this.MQConfig.Port;

            this.Connection = factory.CreateConnection();
            this.Channel = Connection.CreateModel();
            this.Consumer = new EventingBasicConsumer(this.Channel);
            
            ((EventingBasicConsumer)this.Consumer).Received += (s, e) => {
                msgPool.Add(e);

                if (this.OnMessage != null)
                {
                    this.OnMessage(msgPool.Take(), this.Channel);
                }

                this.Channel.BasicAck(e.DeliveryTag, false);
            };
        }

        public void Disconnect()
        {
            this.Channel.Close();
            this.Connection.Close();

            this.Channel.Dispose();
            this.Connection.Dispose();

            this.Channel = null;
            this.Connection = null; 
        }

        public void Send(string exchange, string routeKey, byte[] body , IBasicProperties properties = null)
        { 
            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            //var buffer = Encoding.UTF8.GetBytes(json);

            this.Channel.BasicPublish(exchange, routeKey, properties, body);
        }

        public void ReceiveAsync(string queueName)
        {
            this.Channel.BasicConsume(queueName, false, this.Consumer);
        }

        public IEnumerable<BasicDeliverEventArgs> Receive(string queueName)
        {
            this.Channel.BasicConsume(queueName, false, this.Consumer);

            while (true)
            {
                var ea = msgPool.Take();

                yield return ea;
            }
        }
    }
}
