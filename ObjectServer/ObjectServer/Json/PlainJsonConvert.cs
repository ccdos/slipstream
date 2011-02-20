﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace ObjectServer.Json
{
    public static class PlainJsonConvert
    {
        public static string SerializeObject(object obj)
        {
            var debug = ObjectServerStarter.Configuration.Debug;
            var fmt = Formatting.None;
            if (debug)
            {
                fmt = Formatting.Indented;
            }
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(obj, fmt);

            return str;
        }

        public static object DeserializeObject(string json)
        {
            using (var ss = new StringReader(json))
            {
                return Deserialize(ss);
            }
        }

        public static object Deserialize(TextReader tr)
        {
            using (var jreader = new JsonTextReader(tr))
            {
                return DeserializeInternal(jreader);
            }
        }

        public static object Deserialize(Stream s)
        {
            using (var tr = new StreamReader(s, Encoding.UTF8))
            {
                return Deserialize(tr);
            }
        }

        private static object DeserializeInternal(JsonReader reader)
        {
            reader.Read();
            return ReadToken(reader);
        }

        private static object ReadToken(JsonReader reader)
        {
            object result = null;

            switch (reader.TokenType)
            {
                //跳过注释
                case JsonToken.Comment:
                    SkipComment(reader);
                    break;

                case JsonToken.StartObject:
                    result = ReadObject(reader);
                    break;

                case JsonToken.StartArray:
                    result = ReadArray(reader);
                    break;

                //标量
                case JsonToken.Boolean:
                case JsonToken.Bytes:
                case JsonToken.Date:
                case JsonToken.Null:
                case JsonToken.Float:
                case JsonToken.Integer:
                case JsonToken.String:
                    result = reader.Value;
                    break;


                case JsonToken.Undefined:
                case JsonToken.None:
                default:
                    throw new NotSupportedException();
            }

            return result;
        }

        private static void SkipComment(JsonReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.Comment)
            {
            }
        }


        private static Dictionary<string, object> ReadObject(JsonReader reader)
        {
            Dictionary<string, object> propBag = new Dictionary<string, object>();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var key = (string)reader.Value;
                    reader.Read();
                    object e = ReadToken(reader);
                    propBag[key] = e;
                    continue;
                }
            }

            return propBag;
        }

        private static object[] ReadArray(JsonReader reader)
        {
            var list = new List<object>();

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                object e = ReadToken(reader);
                list.Add(e);
            }

            return list.ToArray();
        }

    }
}