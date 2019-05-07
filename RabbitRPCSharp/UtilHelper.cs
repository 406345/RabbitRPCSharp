using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RabbitRPCSharp
{
    public class UtilHelper
    {
        public class BSON
        {
            public static byte[] SerializeData(object data)
            {
                if( data == null)
                {
                    data = new object();
                }

                MemoryStream ms = new MemoryStream();

                using (BsonWriter writer = new BsonWriter(ms))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(writer, data);
                }

                return ms.ToArray();
            }

            public static T DeserializeData<T>(byte[] data)
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
}
