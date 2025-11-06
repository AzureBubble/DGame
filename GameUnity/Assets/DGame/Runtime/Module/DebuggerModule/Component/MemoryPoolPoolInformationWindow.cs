using System;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class MemoryPoolPoolInformationWindow : ScrollableDebuggerWindowBase
        {
            private readonly Dictionary<string, List<MemoryCollectorInfo>> m_memoryPoolInfos =
                new Dictionary<string, List<MemoryCollectorInfo>>(StringComparer.Ordinal);

            private readonly Comparison<MemoryCollectorInfo> m_normalClassNameComparer = NormalClassNameComparer;
            private readonly Comparison<MemoryCollectorInfo> m_fullClassNameComparer = FullClassNameComparer;
            private bool m_showFullClassName = false;

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Memory Pool Information</b>");
                GUILayout.BeginVertical("box");
                {
                    // 强制检查开关
                    DrawItem("Enable Strict Check", MemoryPool.EnableStrictCheck.ToString(), "强制检查开关");
                    // 获取内存收集对象数量
                    DrawItem("Memory Pool Count", MemoryPool.Capacity.ToString(), "内存收集对象数量");
                }
                GUILayout.EndVertical();

                m_showFullClassName = GUILayout.Toggle(m_showFullClassName, "Show Full Class Name");
                m_memoryPoolInfos.Clear();
                var memoryPoolInfos = MemoryPool.GetAllMemoryCollectorInfos();

                foreach (var memoryPoolInfo in memoryPoolInfos)
                {
                    string assemblyName = memoryPoolInfo.ClassType.Assembly.GetName().Name;
                    List<MemoryCollectorInfo> results = null;

                    if (!m_memoryPoolInfos.TryGetValue(assemblyName, out results))
                    {
                        results = new List<MemoryCollectorInfo>();
                        m_memoryPoolInfos.Add(assemblyName, results);
                    }

                    results.Add(memoryPoolInfo);
                }

                foreach (KeyValuePair<string, List<MemoryCollectorInfo>> assemblyMemoryPoolInfo in m_memoryPoolInfos)
                {
                    GUILayout.Label(Utility.StringUtil.Format("<b>Assembly: {0}</b>", assemblyMemoryPoolInfo.Key));
                    GUILayout.BeginVertical("box");
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(m_showFullClassName ? "<b>Full Class Name</b>" : "<b>Class Name</b>");
                            GUILayout.Label("<b>Unused</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Using</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Spawn</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Recycle</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Add</b>", GUILayout.Width(60f));
                            GUILayout.Label("<b>Remove</b>", GUILayout.Width(60f));
                        }
                        GUILayout.EndHorizontal();

                        if (assemblyMemoryPoolInfo.Value.Count > 0)
                        {
                            assemblyMemoryPoolInfo.Value.Sort(m_showFullClassName
                                ? m_fullClassNameComparer
                                : m_normalClassNameComparer);

                            foreach (var memoryPoolInfo in assemblyMemoryPoolInfo.Value)
                            {
                                DrawMemoryPoolInfo(memoryPoolInfo);
                            }
                        }
                        else
                        {
                            GUILayout.Label("<i>Memory Pool is Empty ...</i>");
                        }
                    }
                    GUILayout.EndVertical();
                }
            }

            private void DrawMemoryPoolInfo(MemoryCollectorInfo memoryPoolInfo)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(m_showFullClassName ? memoryPoolInfo.ClassType.FullName : memoryPoolInfo.ClassType.Name);
                    GUILayout.Label(memoryPoolInfo.UnusedCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(memoryPoolInfo.UsingCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(memoryPoolInfo.SpawnCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(memoryPoolInfo.RecycleCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(memoryPoolInfo.AddCount.ToString(), GUILayout.Width(60f));
                    GUILayout.Label(memoryPoolInfo.RemoveCount.ToString(), GUILayout.Width(60f));
                }
                GUILayout.EndHorizontal();
            }

            private static int NormalClassNameComparer(MemoryCollectorInfo a, MemoryCollectorInfo b)
                => String.Compare(a.ClassType.Name, b.ClassType.Name, StringComparison.Ordinal);

            private static int FullClassNameComparer(MemoryCollectorInfo a, MemoryCollectorInfo b)
                => String.Compare(a.ClassType.FullName, b.ClassType.FullName, StringComparison.Ordinal);
        }
    }
}