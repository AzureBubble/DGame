using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class G
    {
        public static string R(string content)
        {
            return content;
        }

        public static string R<T>(string content, T args)
        {
            return DGame.Utility.StringUtil.Format(content, args);
        }
    }

    public class Language
    {
        public void Test()
        {
            string str = G.R("{0}测试", "这是");
        }
    }
}