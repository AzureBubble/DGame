using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DGame;

namespace AOT
{
    public static class RuntimeInitializeOnLoadMethodCollector
    {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
        private static string s_runtimeInitializeOnLoadMethodCollectorClassName = "AOT.RuntimeInitializeOnLoadMethodCollector_Gen";
        private static string s_executeMethodsName = "ExecuteMethods";
#endif

        public static void ExecuteMethods(RuntimeInitializeLoadType loadType)
        {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
            if (!Settings.UpdateSettings.Enable)
            {
                return;
            }
            var collectorType = Type.GetType(s_runtimeInitializeOnLoadMethodCollectorClassName);

            if (collectorType != null)
            {
                var executeMethod = collectorType.GetMethod(s_executeMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                executeMethod?.Invoke(null, new object[] { loadType });
            }
#endif
        }
    }

    public static class RequireComponentCollector
    {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
        private static string s_requireComponentCollectorClassName = "AOT.RequireComponentCollector_Gen";
        private static string s_executeMethodsName = "AddComponents";
#endif

        public static void ExecuteMethods()
        {
#if !UNITY_EDITOR && ENABLE_HYBRIDCLR
            if (!Settings.UpdateSettings.Enable)
            {
                return;
            }
            var collectorType = Type.GetType(s_requireComponentCollectorClassName);

            if (collectorType != null)
            {
                var executeMethod = collectorType.GetMethod(s_executeMethodsName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                executeMethod?.Invoke(null, null);
            }
#endif
        }
    }
}