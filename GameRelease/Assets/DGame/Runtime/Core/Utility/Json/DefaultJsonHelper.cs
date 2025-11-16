using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 默认的 Json 辅助器 使用 UnityEngine 的 JsonUtility
    /// </summary>
    public class DefaultJsonHelper : Utility.IJsonHelper
    {
        public string ToJson(object obj) => JsonUtility.ToJson(obj, true);

        public T ToObject<T>(string json) => JsonUtility.FromJson<T>(json);

        public object ToObject(string json, Type objectType) => JsonUtility.FromJson(json, objectType);
    }
}