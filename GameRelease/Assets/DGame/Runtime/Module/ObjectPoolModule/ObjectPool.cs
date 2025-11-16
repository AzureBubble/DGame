using System;
using System.Collections.Generic;

namespace DGame
{
    internal sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 内部泛型对象池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class ObjectPool<T> : BaseObjectPool, IObjectPool<T> where T : BasePoolObject
        {
            /// <summary>
            /// key -> 对象名称 value -> 内部对象池对象
            /// </summary>
            private readonly DGameMultiDictionary<string, PoolObject<T>> m_poolObjects;

            /// <summary>
            /// key -> 资源对象 value -> 内部对象池对象
            /// </summary>
            private readonly Dictionary<object, PoolObject<T>> m_poolObjectsMap;

            private readonly ReleaseCanRecycleObjectFilterCallback<T> m_defaultReleaseCanRecycleObjectFilterCallback;
            private readonly List<T> m_cachedCanRecycleObjects;
            private readonly List<T> m_cachedToRecycleObjects;
            private readonly bool m_allowMultiSpawn;
            private int m_capacity;
            private float m_expireTime;
            private float m_autoReleaseTime;

            /// <summary>
            /// 对象池对象类型
            /// </summary>
            public override Type ObjectType => typeof(T);

            /// <summary>
            /// 对象池中对象数量
            /// </summary>
            public override int Count => m_poolObjectsMap.Count;

            public bool AllowMultipleSpawn { get; }

            public override int CanRecycleToMemoryPoolCount
            {
                get
                {
                    GetCanRecycleObject(m_cachedCanRecycleObjects);
                    return m_cachedCanRecycleObjects.Count;
                }
            }

            public override bool AllowMultiSpawn => m_allowMultiSpawn;
            public override float AutoReleaseInterval { get; set; }

            public override int Capacity
            {
                get => m_capacity;
                set
                {
                    if (value < 0)
                    {
                        throw new DGameException("对象池容量异常：小于0");
                    }

                    if (value == m_capacity)
                    {
                        return;
                    }

                    m_capacity = value;
                    ReleaseCanRecycleObject();
                }
            }

            public override float ExpireTime
            {
                get => m_expireTime;
                set
                {
                    if (value < 0f)
                    {
                        throw new DGameException("无效的过期时间：小于0");
                    }

                    if (Math.Abs(ExpireTime - value) < 0.01f)
                    {
                        return;
                    }

                    m_expireTime = value;
                    ReleaseCanRecycleObject();
                }
            }

            public override int Priority { get; set; }

            public ObjectPool(string name, bool allowMultipleSpawn, float autoReleaseInterval, int capacity,
                float expireTime, int priority) : base(name)
            {
                m_poolObjects = new DGameMultiDictionary<string, PoolObject<T>>();
                m_poolObjectsMap = new Dictionary<object, PoolObject<T>>();
                m_defaultReleaseCanRecycleObjectFilterCallback = DefaultReleaseCanRecycleObjectFilterCallback;
                m_cachedCanRecycleObjects = new List<T>();
                m_cachedToRecycleObjects = new List<T>();
                m_allowMultiSpawn = allowMultipleSpawn;
                AutoReleaseInterval = autoReleaseInterval;
                Capacity = capacity;
                ExpireTime = expireTime;
                Priority = priority;
                m_autoReleaseTime = 0f;
            }

            public void Register(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new DGameException("注册的对象无效");
                }

                PoolObject<T> poolObject = PoolObject<T>.CreateFromMemoryPool(obj, spawned);
                m_poolObjects.Add(obj.Name, poolObject);
                m_poolObjectsMap.Add(obj.Target, poolObject);

                if (Count > m_capacity)
                {
                    ReleaseCanRecycleObject();
                }
            }

            public bool CanSpawn()
            {
                return CanSpawn(string.Empty);
            }

            public bool CanSpawn(string name)
            {
                if (name == null)
                {
                    throw new DGameException("对象名称无效");
                }

                if (m_poolObjects.TryGetValue(name, out var objectRange))
                {
                    foreach (var poolObject in objectRange)
                    {
                        // 对象如果正在被使用则需要判断是否可以被多次获取
                        if (m_allowMultiSpawn || !poolObject.IsUsing)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public T Spawn()
            {
                return Spawn(string.Empty);
            }

            public T Spawn(string name)
            {
                if (name == null)
                {
                    throw new DGameException("对象名称无效");
                }

                if (m_poolObjects.TryGetValue(name, out var objectRange))
                {
                    foreach (var poolObject in objectRange)
                    {
                        // 对象如果正在被使用则需要判断是否可以被多次获取
                        if (m_allowMultiSpawn || !poolObject.IsUsing)
                        {
                            return poolObject.OnSpawnFromObjectPool();
                        }
                    }
                }

                return null;
            }

            public void Recycle(T obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                Recycle(obj.Target);
            }

            public void Recycle(object obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                // 尝试从对象池中获取
                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.OnRecycleToObjectPool();

                    if (Count > m_capacity && poolObject.SpawnCount <= 0)
                    {
                        ReleaseCanRecycleObject();
                    }
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public void SetLocked(T obj, bool locked)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                SetLocked(obj.Target, locked);
            }

            public void SetLocked(object obj, bool locked)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.Locked = locked;
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public void SetPriority(T obj, int priority)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                SetPriority(obj.Target, priority);
            }

            public void SetPriority(object obj, int priority)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject != null)
                {
                    poolObject.Priority = priority;
                }
                else
                {
                    throw new DGameException(Utility.StringUtil.Format("对象池中无此对象{0}，对象类型：{1}，对象的值是{2}",
                        new TypeNamePair(typeof(T), Name), obj.GetType().FullName, obj));
                }
            }

            public bool RecycleToMemoryPool(T obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                return RecycleToMemoryPool(obj.Target);
            }

            public bool RecycleToMemoryPool(object obj)
            {
                if (obj == null)
                {
                    throw new DGameException("对象无效");
                }

                var poolObject = GetPoolObject(obj);

                if (poolObject == null)
                {
                    return false;

                }

                if (poolObject.IsUsing || poolObject.Locked || !poolObject.CustomCanReleaseFlag)
                {
                    return false;
                }

                m_poolObjects.Remove(poolObject.Name, poolObject);
                m_poolObjectsMap.Remove(poolObject.Peek().Target);
                poolObject.ReleaseObj(false);
                MemoryPool.Recycle(poolObject);
                return true;
            }

            public override void ReleaseCanRecycleObject()
            {
                ReleaseCanRecycleObject(Count - m_capacity, m_defaultReleaseCanRecycleObjectFilterCallback);
            }

            public override void ReleaseCanRecycleObject(int releaseCnt)
            {
                ReleaseCanRecycleObject(releaseCnt, m_defaultReleaseCanRecycleObjectFilterCallback);
            }

            public void ReleaseCanRecycleObject(ReleaseCanRecycleObjectFilterCallback<T> releaseFilterCallback)
            {
                ReleaseCanRecycleObject(Count - m_capacity, releaseFilterCallback);
            }

            public void ReleaseCanRecycleObject(int releaseCnt,
                ReleaseCanRecycleObjectFilterCallback<T> releaseFilterCallback)
            {
                if (releaseFilterCallback == null)
                {
                    throw new DGameException("释放对象池对象的过滤回调函数无效");
                }

                if (releaseCnt <= 0)
                {
                    releaseCnt = 0;
                }

                DateTime expireTime = DateTime.MinValue;

                if (m_expireTime < float.MaxValue)
                {
                    expireTime = DateTime.UtcNow.AddSeconds(-m_expireTime);
                }

                m_autoReleaseTime = 0.0f;
                GetCanRecycleObject(m_cachedCanRecycleObjects);
                List<T> toReleaseObjs = releaseFilterCallback(m_cachedCanRecycleObjects, releaseCnt, expireTime);

                if (toReleaseObjs == null || toReleaseObjs.Count <= 0)
                {
                    return;
                }

                foreach (var releaseObj in toReleaseObjs)
                {
                    RecycleToMemoryPool(releaseObj);
                }
            }

            public override void ReleaseAllUnusedToMemoryPool()
            {
                m_autoReleaseTime = 0.0f;
                GetCanRecycleObject(m_cachedCanRecycleObjects);

                foreach (var recycleObject in m_cachedCanRecycleObjects)
                {
                    RecycleToMemoryPool(recycleObject);
                }
            }

            internal override void Update(float elapsedSeconds, float realElapseSeconds)
            {
                m_autoReleaseTime += realElapseSeconds;

                if (m_autoReleaseTime < AutoReleaseInterval)
                {
                    return;
                }

                ReleaseCanRecycleObject();
            }

            public override PoolObjectInfo[] GetAllPoolObjectInfos()
            {
                List<PoolObjectInfo> poolObjectInfos = new List<PoolObjectInfo>();

                foreach (var objRanges in m_poolObjects)
                {
                    foreach (var poolObject in objRanges.Value)
                    {
                        poolObjectInfos.Add(new PoolObjectInfo(poolObject.Name, poolObject.Locked,
                            poolObject.CustomCanReleaseFlag,
                            poolObject.Priority, poolObject.LastUseTime, poolObject.SpawnCount));
                    }
                }

                return poolObjectInfos.ToArray();
            }

            internal override void Destroy()
            {
                foreach (var poolObj in m_poolObjectsMap.Values)
                {
                    poolObj.ReleaseObj(true);
                    MemoryPool.Recycle(poolObj);
                }

                m_poolObjectsMap.Clear();
                m_poolObjects.Clear();
                m_cachedCanRecycleObjects.Clear();
                m_cachedToRecycleObjects.Clear();
            }

            private PoolObject<T> GetPoolObject(object target)
            {
                if (target == null)
                {
                    throw new DGameException("目标对象无效");
                }

                if (m_poolObjectsMap.TryGetValue(target, out var poolObject))
                {
                    return poolObject;
                }

                return null;
            }

            private void GetCanRecycleObject(List<T> result)
            {
                if (result == null)
                {
                    throw new DGameException("传入的列表参数无效");
                }

                result.Clear();

                foreach (var poolObject in m_poolObjectsMap.Values)
                {
                    if (poolObject.IsUsing || poolObject.Locked || !poolObject.CustomCanReleaseFlag)
                    {
                        continue;
                    }

                    result.Add(poolObject.Peek());
                }
            }

            private List<T> DefaultReleaseCanRecycleObjectFilterCallback(List<T> candidateObjects, int toReleaseCount,
                DateTime expireTime)
            {
                m_cachedToRecycleObjects.Clear();

                if (expireTime > DateTime.MinValue)
                {
                    for (int i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        var poolObject = candidateObjects[i];

                        if (poolObject.LastUseTime <= expireTime)
                        {
                            m_cachedToRecycleObjects.Add(poolObject);
                            candidateObjects.RemoveAt(i);
                        }
                    }

                    toReleaseCount -= m_cachedToRecycleObjects.Count;
                }

                candidateObjects.Sort(SortCandidateObjects);

                for (int i = 0; i < candidateObjects.Count && toReleaseCount > 0; i++)
                {
                    m_cachedToRecycleObjects.Add(candidateObjects[i]);
                    toReleaseCount--;
                }

                return m_cachedToRecycleObjects;
            }

            private int SortCandidateObjects(T a, T b)
            {
                int priorityCompare = b.Priority.CompareTo(a.Priority);
                return priorityCompare != 0 ? priorityCompare : a.LastUseTime.CompareTo(b.LastUseTime);
            }
        }
    }
}