using UnityEngine;
using UnityEngine.Profiling;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class ProfilerInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Profiler Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Supported", Profiler.supported.ToString(), "性能分析器是否在当前平台支持");
                    DrawItem("Enabled", Profiler.enabled.ToString(), "性能分析器是否已启用");
                    DrawItem("Enable Binary Log",
                        Profiler.enableBinaryLog ? Utility.StringUtil.Format("True, {0}", Profiler.logFile) : "False", "是否启用二进制日志记录");
#if UNITY_2019_3_OR_NEWER
                    DrawItem("Enable Allocation Callstacks", Profiler.enableAllocationCallstacks.ToString(), "是否启用分配调用栈跟踪");
#endif
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Area Count", Profiler.areaCount.ToString(), "性能分析区域数量");
#endif
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Max Used Memory", GetByteLengthString(Profiler.maxUsedMemory), "应用程序运行至今使用的最大内存量");
#endif
                    DrawItem("Mono Used Size", GetByteLengthString(Profiler.GetMonoUsedSizeLong()), "Mono虚拟机已使用的堆内存大小");
                    DrawItem("Mono Heap Size", GetByteLengthString(Profiler.GetMonoHeapSizeLong()), "Mono虚拟机堆的总大小");
                    DrawItem("Used Heap Size", GetByteLengthString(Profiler.usedHeapSizeLong), "整个堆内存的使用量");
                    DrawItem("Total Allocated Memory", GetByteLengthString(Profiler.GetTotalAllocatedMemoryLong()), "应用程序当前分配的总内存");
                    DrawItem("Total Reserved Memory", GetByteLengthString(Profiler.GetTotalReservedMemoryLong()), "操作系统为应用程序保留的总内存");
                    DrawItem("Total Unused Reserved Memory",
                        GetByteLengthString(Profiler.GetTotalUnusedReservedMemoryLong()), "已保留但未使用的内存");
#if UNITY_2018_1_OR_NEWER
                    DrawItem("Allocated Memory For Graphics Driver",
                        GetByteLengthString(Profiler.GetAllocatedMemoryForGraphicsDriver()), "图形驱动程序分配的内存");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Temp Allocator Size", GetByteLengthString(Profiler.GetTempAllocatorSize()), "临时分配器的大小");
#endif
                    DrawItem("Marshal Cached HGlobal Size", GetByteLengthString(Utility.Marshal.CachedHGlobalSize), "封送处理缓存的非托管内存大小");
                }
                GUILayout.EndVertical();
            }
        }
    }
}