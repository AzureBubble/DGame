using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AOT
{
    public static class RuntimeInitializeOnLoadMethodCollector
    {
        private static string s_runtimeInitializeOnLoadMethodCollectorClassName = "AOT.RuntimeInitializeOnLoadMethodCollector_Gen";
        private static string s_ExecuteMethodsName = "ExecuteMethods";

        public static void ExecuteMethods(RuntimeInitializeLoadType loadType)
        {
            var collectorType = Type.GetType(s_runtimeInitializeOnLoadMethodCollectorClassName);

            if (collectorType != null)
            {
                var executeMethod = collectorType.GetMethod(s_ExecuteMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                executeMethod?.Invoke(null, new object[] { loadType });
            }
        }
    }
}