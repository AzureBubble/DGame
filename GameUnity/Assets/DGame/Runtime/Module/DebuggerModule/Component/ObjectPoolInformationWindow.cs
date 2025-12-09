using System.Globalization;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class ObjectPoolInformationWindow : ScrollableDebuggerWindowBase
        {
            private IObjectPoolModule m_objectPool = null;

            public override void Initialize(params object[] args)
            {
                m_objectPool = ModuleSystem.GetModule<IObjectPoolModule>();
                if (m_objectPool == null)
                {
                    DLogger.Fatal("Object pool component is invalid.");
                }
            }

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Object Pool Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Object Pool Count", m_objectPool.Count.ToString(), "对象池数量");
                }
                GUILayout.EndVertical();
                BaseObjectPool[] objectPools = m_objectPool.GetAllObjectPools(true);
                for (int i = 0; i < objectPools.Length; i++)
                {
                    DrawObjectPool(objectPools[i]);
                }
            }

            private void DrawObjectPool(BaseObjectPool objectPool)
            {
                GUILayout.Label(Utility.StringUtil.Format("<b>Object Pool: {0}</b>", objectPool.FullName));
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Name", objectPool.Name, "对象池名称");
                    DrawItem("Type", objectPool.ObjectType.FullName, "对象池类型");
                    DrawItem("Auto Release Interval", objectPool.AutoReleaseInterval.ToString(CultureInfo.InvariantCulture), "自动释放可释放对象的间隔（秒）");
                    DrawItem("Capacity", objectPool.Capacity.ToString(), "对象池容量");
                    DrawItem("Used Count", objectPool.Count.ToString(), "对象的数量");
                    DrawItem("Can Release Count", objectPool.CanReleaseCount.ToString(), "可以释放的对象数量 （回收到内存池）");
                    DrawItem("Expire Time", objectPool.ExpireTime.ToString(CultureInfo.InvariantCulture), "对象池对象过期回收时间（秒）");
                    DrawItem("Priority", objectPool.Priority.ToString(), "对象池优先级");
                    PoolObjectInfo[] objectInfos = objectPool.GetAllPoolObjectInfos();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("<b>Name</b>");
                        GUILayout.Label("<b>Locked</b>", GUILayout.Width(60f));
                        GUILayout.Label(objectPool.AllowMultiSpawn ? "<b>Count</b>" : "<b>In Use</b>", GUILayout.Width(60f));
                        GUILayout.Label("<b>Flag</b>", GUILayout.Width(60f));
                        GUILayout.Label("<b>Priority</b>", GUILayout.Width(60f));
                        GUILayout.Label("<b>Last Use Time</b>", GUILayout.Width(120f));
                    }
                    GUILayout.EndHorizontal();

                    if (objectInfos.Length > 0)
                    {
                        for (int i = 0; i < objectInfos.Length; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(string.IsNullOrEmpty(objectInfos[i].Name) ? "<None>" : objectInfos[i].Name);
                                GUILayout.Label(objectInfos[i].Locked.ToString(), GUILayout.Width(60f));
                                GUILayout.Label(objectPool.AllowMultiSpawn ? objectInfos[i].SpawnCount.ToString() : objectInfos[i].IsUsing.ToString(), GUILayout.Width(60f));
                                GUILayout.Label(objectInfos[i].CustomCanReleaseFlag.ToString(), GUILayout.Width(60f));
                                GUILayout.Label(objectInfos[i].Priority.ToString(), GUILayout.Width(60f));
                                GUILayout.Label(objectInfos[i].LastUseTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), GUILayout.Width(120f));
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        GUILayout.Label("<i>Object Pool is Empty ...</i>");
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}