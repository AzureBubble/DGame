using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DGame
{
    public class NewtonsoftJsonHelper : Utility.IJsonHelper
    {
        public string ToJson(object obj) => JsonConvert.SerializeObject(obj);

        public T ToObject<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        public object ToObject(string json, Type objectType) => JsonConvert.DeserializeObject(json, objectType);
    }
}