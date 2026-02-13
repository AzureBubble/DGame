using System;
using System.Collections.Generic;

namespace DGame
{
    internal sealed partial class ObjectPoolModule : Module, IObjectPoolModule, IUpdateModule
    {
        private const int DEFAULT_PRIORITY = 0;
        private const int DEFAULT_CAPACITY = int.MaxValue;
        private const float DEFAULT_EXPIRE_TIME = float.MaxValue;

        private readonly Dictionary<TypeNamePair, BaseObjectPool> m_objectPools;
        private readonly List<BaseObjectPool> m_cachedAllObjectPools;
        private readonly Comparison<BaseObjectPool> m_objectPoolComparison;

        public override int Priority => 6;

        public int Count => m_objectPools.Count;

        public ObjectPoolModule()
        {
            m_objectPools = new Dictionary<TypeNamePair, BaseObjectPool>();
            m_cachedAllObjectPools = new List<BaseObjectPool>();
            m_objectPoolComparison = ObjectPoolComparison;
        }

        private int ObjectPoolComparison(BaseObjectPool x, BaseObjectPool y)
            => x.Priority.CompareTo(y.Priority);

        public override void OnCreate()
        {
            // DLogger.Info("======== 对象池系统初始化 ========");
        }

        public override void OnDestroy()
        {
            for (var i = 0; i < m_cachedAllObjectPools.Count; i++)
            {
                m_cachedAllObjectPools[i].Destroy();
            }

            m_objectPools.Clear();
            m_cachedAllObjectPools.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (BaseObjectPool pool in m_cachedAllObjectPools)
            {
                pool.Update(elapseSeconds, realElapseSeconds);
            }
        }

        #region Contains

        private bool InternalContains(TypeNamePair pair) => m_objectPools.ContainsKey(pair);

        public bool Contains<T>() where T : BasePoolObject => InternalContains(new TypeNamePair(typeof(T)));

        public bool Contains(Type objectType)
        {
            if (objectType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objectType))
            {
                throw new DGameException(Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objectType.FullName));
            }
            return InternalContains(new TypeNamePair(objectType));
        }

        public bool Contains<T>(string name) where T : BasePoolObject
            => InternalContains(new TypeNamePair(typeof(T), name));

        public bool Contains(Type objectType, string name)
        {
            if (objectType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objectType))
            {
                throw new DGameException(Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objectType.FullName));
            }
            return InternalContains(new TypeNamePair(objectType, name));
        }

        public bool Contains(Predicate<BaseObjectPool> condition)
        {
            if (condition == null)
            {
                throw new DGameException("条件无效");
            }

            foreach (var pool in m_objectPools.Values)
            {
                if (condition(pool))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region GetObjectPool

        private BaseObjectPool InternalGetObjectPool(TypeNamePair pair)
            => m_objectPools.GetValueOrDefault(pair);

        public IObjectPool<T> GetObjectPool<T>() where T : BasePoolObject
            => InternalGetObjectPool(new TypeNamePair(typeof(T))) as IObjectPool<T>;

        public BaseObjectPool GetObjectPool(Type objType)
        {
            if (objType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objType))
            {
                throw new DGameException(Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objType.FullName));
            }
            return InternalGetObjectPool(new TypeNamePair(objType));
        }

        public IObjectPool<T> GetObjectPool<T>(string name) where T : BasePoolObject
            => InternalGetObjectPool(new TypeNamePair(typeof(T), name)) as IObjectPool<T>;

        public BaseObjectPool GetObjectPool(Type objType, string name)
        {
            if (objType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objType))
            {
                throw new DGameException(Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objType.FullName));
            }
            return InternalGetObjectPool(new TypeNamePair(objType, name));
        }

        public BaseObjectPool GetObjectPool(Predicate<BaseObjectPool> condition)
        {
            if (condition == null)
            {
                throw new DGameException("条件无效");
            }

            foreach (var pool in m_objectPools.Values)
            {
                if (condition(pool))
                {
                    return pool;
                }
            }
            return null;
        }

        public BaseObjectPool[] GetObjectPools(Predicate<BaseObjectPool> condition)
        {
            List<BaseObjectPool> pools = new List<BaseObjectPool>();
            GetObjectPools(condition, pools);
            return pools.ToArray();
        }

        public void GetObjectPools(Predicate<BaseObjectPool> condition, List<BaseObjectPool> results)
        {
            if (condition == null)
            {
                throw new DGameException("条件无效");
            }

            if (results == null)
            {
                throw new DGameException("Results is invalid.");
            }
            results.Clear();
            foreach (var pool in m_objectPools.Values)
            {
                if (condition(pool))
                {
                    results.Add(pool);
                }
            }
        }

        public BaseObjectPool[] GetAllObjectPools()
            => GetAllObjectPools(false);

        public void GetAllObjectPools(List<BaseObjectPool> pools)
            => GetAllObjectPools(false, pools);

        public BaseObjectPool[] GetAllObjectPools(bool sort)
        {
            if (sort)
            {
                List<BaseObjectPool> pools = new List<BaseObjectPool>();

                foreach (var pool in m_objectPools.Values)
                {
                    pools.Add(pool);
                }
                pools.Sort(m_objectPoolComparison);
                return pools.ToArray();
            }
            else
            {
                int index = 0;
                BaseObjectPool[] pools = new BaseObjectPool[m_objectPools.Count];

                foreach (var pool in m_objectPools.Values)
                {
                    pools[index++] = pool;
                }
                return pools;
            }
        }

        public void GetAllObjectPools(bool sort, List<BaseObjectPool> pools)
        {
            if (pools == null)
            {
                throw new DGameException("传入的参数列表无效");
            }
            pools.Clear();
            foreach (var pool in m_objectPools.Values)
            {
                pools.Add(pool);
            }
            if (sort)
            {
                pools.Sort(m_objectPoolComparison);
            }
        }

        #endregion

        #region CreateSingleSpawnObjectPool

        private IObjectPool<T> InternalCreateObjectPool<T>(string name, bool allowMultiSpawn, float autoReleaseInterval,
            int capacity, float expireTime, int priority) where T : BasePoolObject
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (Contains<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("对象池已经存在 '{0}'", pair));
            }

            var pool = new ObjectPool<T>(name, allowMultiSpawn, autoReleaseInterval, capacity, expireTime, priority);

            if (m_objectPools.TryAdd(pair, pool))
            {
                return pool;
            }
            return null;
        }

        private BaseObjectPool InternalCreateObjectPool(Type objType, string name, bool allowMultiSpawn, float autoReleaseInterval,
            int capacity, float expireTime, int priority)
        {
            if (objType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objType))
            {
                throw new DGameException(Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objType.FullName));
            }
            TypeNamePair pair = new TypeNamePair(objType, name);

            if (Contains(objType, name))
            {
                throw new DGameException(Utility.StringUtil.Format("对象池已经存在 '{0}'", pair));
            }

            Type objectPoolType = typeof(ObjectPool<>).MakeGenericType(objType);
            var pool = Activator.CreateInstance(objectPoolType, name, allowMultiSpawn,
                autoReleaseInterval, capacity, expireTime, priority) as BaseObjectPool;

            if (m_objectPools.TryAdd(pair, pool))
            {
                return pool;
            }
            return null;
        }

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>() where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType)
            => InternalCreateObjectPool(objType, string.Empty, false,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false, DEFAULT_EXPIRE_TIME,
                DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name)
            => InternalCreateObjectPool(objType, name, false,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity)
            => InternalCreateObjectPool(objType, string.Empty, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, float expireTime)
            => InternalCreateObjectPool(objType, string.Empty, false,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false, DEFAULT_EXPIRE_TIME,
                capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity)
            => InternalCreateObjectPool(objType, name, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false, expireTime,
                DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float expireTime)
            => InternalCreateObjectPool(objType, name, false,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, float expireTime)
            => InternalCreateObjectPool(objType, string.Empty, false,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, int priority)
            => InternalCreateObjectPool(objType, string.Empty, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(float expireTime, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, float expireTime, int priority)
            => InternalCreateObjectPool(objType, string.Empty, false,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, float expireTime)
            => InternalCreateObjectPool(objType, name, false,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, int priority)
            => InternalCreateObjectPool(objType, name, false,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float expireTime, int priority)
            => InternalCreateObjectPool(objType, name, false,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(int capacity, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, false,
                expireTime, capacity, expireTime, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, int capacity, float expireTime, int priority)
            => InternalCreateObjectPool(objType, string.Empty, false,
                expireTime, capacity, expireTime, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false,
                expireTime, capacity, expireTime, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, int capacity, float expireTime,
            int priority)
            => InternalCreateObjectPool(objType, name, false,
                expireTime, capacity, expireTime, priority);

        public IObjectPool<T> CreateSingleSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity,
            float expireTime, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, false,
                autoReleaseInterval, capacity, expireTime, priority);

        public BaseObjectPool CreateSingleSpawnObjectPool(Type objType, string name, float autoReleaseInterval,
            int capacity, float expireTime, int priority)
            => InternalCreateObjectPool(objType, name, false,
                autoReleaseInterval, capacity, expireTime, priority);

        #endregion

        #region CreateMultiSpawnObjectPool

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>() where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType)
            => InternalCreateObjectPool(objType, string.Empty, true,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);


        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name)
            => InternalCreateObjectPool(objType, name, true,
                DEFAULT_EXPIRE_TIME, DEFAULT_CAPACITY, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);


        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity)
            => InternalCreateObjectPool(objType, string.Empty, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, float expireTime)
            => InternalCreateObjectPool(objType, string.Empty, true,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity)
            => InternalCreateObjectPool(objType, name, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float expireTime)
            => InternalCreateObjectPool(objType, name, true,
                expireTime, DEFAULT_CAPACITY, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, float expireTime)
            => InternalCreateObjectPool(objType, string.Empty, true,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, int priority)
            => InternalCreateObjectPool(objType, string.Empty, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(float expireTime, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, float expireTime, int priority)
            => InternalCreateObjectPool(objType, string.Empty, true,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, float expireTime)
            => InternalCreateObjectPool(objType, name, true,
                expireTime, capacity, expireTime, DEFAULT_PRIORITY);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, int priority)
            => InternalCreateObjectPool(objType, name, true,
                DEFAULT_EXPIRE_TIME, capacity, DEFAULT_EXPIRE_TIME, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float expireTime, int priority)
            => InternalCreateObjectPool(objType, name, true,
                expireTime, DEFAULT_CAPACITY, expireTime, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(int capacity, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(string.Empty, true,
                expireTime, capacity, expireTime, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, int capacity, float expireTime, int priority)
            => InternalCreateObjectPool(objType, string.Empty, true,
                expireTime, capacity, expireTime, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, int capacity, float expireTime, int priority)
            where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                expireTime, capacity, expireTime, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, int capacity, float expireTime,
            int priority)
            => InternalCreateObjectPool(objType, name, true,
                expireTime, capacity, expireTime, priority);

        public IObjectPool<T> CreateMultiSpawnObjectPool<T>(string name, float autoReleaseInterval, int capacity,
            float expireTime, int priority) where T : BasePoolObject
            => InternalCreateObjectPool<T>(name, true,
                autoReleaseInterval, capacity, expireTime, priority);

        public BaseObjectPool CreateMultiSpawnObjectPool(Type objType, string name, float autoReleaseInterval,
            int capacity, float expireTime, int priority)
            => InternalCreateObjectPool(objType, name, true,
                autoReleaseInterval, capacity, expireTime, priority);

        #endregion

        #region DestroyObjectPool

        private bool InternalDestroyObjectPool(TypeNamePair pair)
        {
            if (m_objectPools.TryGetValue(pair, out var pool))
            {
                pool.Destroy();
                return m_objectPools.Remove(pair);
            }
            return false;
        }

        public bool DestroyObjectPool<T>() where T : BasePoolObject
            => InternalDestroyObjectPool(new TypeNamePair(typeof(T)));

        public bool DestroyObjectPool(Type objectType)
        {
            if (objectType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objectType))
            {
                throw new DGameException(
                    Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objectType.FullName));
            }

            return InternalDestroyObjectPool(new TypeNamePair(objectType));
        }

        public bool DestroyObjectPool<T>(string name) where T : BasePoolObject
            => InternalDestroyObjectPool(new TypeNamePair(typeof(T), name));

        public bool DestroyObjectPool(Type objectType, string name)
        {
            if (objectType == null)
            {
                throw new DGameException("对象类型无效");
            }

            if (!typeof(BasePoolObject).IsAssignableFrom(objectType))
            {
                throw new DGameException(
                    Utility.StringUtil.Format("对象类型{0}无效，必须是BasePoolObject子类", objectType.FullName));
            }

            return InternalDestroyObjectPool(new TypeNamePair(objectType, name));
        }

        public bool DestroyObjectPool<T>(IObjectPool<T> objectPools) where T : BasePoolObject
        {
            if (objectPools == null)
            {
                throw new DGameException("传入的参数无效");
            }
            return InternalDestroyObjectPool( new TypeNamePair(typeof(T), objectPools.Name));
        }

        public bool DestroyObjectPool(BaseObjectPool objectPools)
        {
            if (objectPools == null)
            {
                throw new DGameException("传入的参数无效");
            }
            return InternalDestroyObjectPool( new TypeNamePair(objectPools.ObjectType, objectPools.Name));
        }

        #endregion

        #region ReleaseCanRecycleObject

        public void Release()
        {
            DLogger.Info("对象池释放中...");
            GetAllObjectPools(true, m_cachedAllObjectPools);

            foreach (var pool in m_cachedAllObjectPools)
            {
                pool.Release();
            }
        }

        public void ReleaseAllUnused()
        {
            DLogger.Info("对象池释放所有未使用的对象中...");
            GetAllObjectPools(true, m_cachedAllObjectPools);

            foreach (var pool in m_cachedAllObjectPools)
            {
                pool.ReleaseAllUnused();
            }
        }

        #endregion
    }
}