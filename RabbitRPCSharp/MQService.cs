using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitRPCSharp
{
    public class MQService
    {
        public string RabbitMQHost { get; set; } = "";
        public int RabbitMQPost { get; set; } = 5672;
        public string RabbitMQUsername { get; set; } = "";
        public string RabbitMQPassword { get; set; } = "";
        public string RabbitMQVirtualHost { get; set; } = "/rpc";

        public IConnection Connection { get; private set; }
        public IModel Channel { get; private set; }

        public void Connect()
        {
            var factory = new RabbitMQ.Client.ConnectionFactory();
            factory.UserName = this.RabbitMQUsername;
            factory.Password = this.RabbitMQPassword;
            factory.VirtualHost = this.RabbitMQVirtualHost;
            factory.HostName = this.RabbitMQHost;
            factory.Port = this.RabbitMQPost;

            this.Connection = factory.CreateConnection();
            this.Channel = Connection.CreateModel();
        }

        public void Send(string exchange, string routeKey, object body)
        {
            var factory = new RabbitMQ.Client.ConnectionFactory();
            factory.UserName = this.RabbitMQUsername;
            factory.Password = this.RabbitMQPassword;
            factory.VirtualHost = this.RabbitMQVirtualHost;
            factory.HostName = this.RabbitMQHost;
            factory.Port = this.RabbitMQPost;

            var connection = factory.CreateConnection();
            var channel = Connection.CreateModel();

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var buffer = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange, routeKey, null, buffer);
        }

        public IEnumerable<BasicDeliverEventArgs> Receive(string queueName)
        {
            var consumer = new QueueingBasicConsumer(this.Channel);
            this.Channel.BasicConsume(queueName, true, consumer);

            while (true)
            {
                var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                yield return ea;
            }
        }
    }
}
