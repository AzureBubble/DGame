namespace GameBattle
{
    /// <summary>
    /// 通过 World + RuntimeId 保存实体引用，并用捕获的实体实例检测对象池复用。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
    public readonly struct EntityRef<T> where T : Entity
    {
        #region Fields

        /// <summary>
        /// 引用所属世界。
        /// </summary>
        private readonly World m_world;

        /// <summary>
        /// 捕获时的运行时唯一 id。
        /// </summary>
        private readonly long m_runtimeId;

        /// <summary>
        /// 捕获时的实体实例，用于检测对象池复用。
        /// </summary>
        private readonly T m_entity;

        #endregion

        #region Properties

        /// <summary>
        /// 捕获时的运行时唯一 id。
        /// </summary>
        public long RuntimeId => m_runtimeId;

        /// <summary>
        /// 当前引用是否能解析到原实体。
        /// </summary>
        public bool IsValid => TryGet(out _);

        /// <summary>
        /// 捕获的实体实例是否已经被对象池复用为另一个 RuntimeId。
        /// </summary>
        public bool IsReused
            => m_entity != null && m_runtimeId != 0 && m_entity.RuntimeId != 0 && m_entity.RuntimeId != m_runtimeId;

        /// <summary>
        /// 当前引用是否已经失效或无法解析。
        /// </summary>
        public bool IsDisposed => m_runtimeId == 0 || m_world == null || !TryGet(out _);

        /// <summary>
        /// 当前引用解析到的实体，失效时返回 null。
        /// </summary>
        public T Value => TryGet(out var entity) ? entity : null;

        #endregion

        #region Create

        /// <summary>
        /// 从实体实例创建引用。
        /// </summary>
        /// <param name="entity">实体实例。</param>
        public EntityRef(T entity)
        {
            m_world = entity?.World;
            m_runtimeId = entity?.RuntimeId ?? 0;
            m_entity = entity;
        }

        /// <summary>
        /// 从世界和 RuntimeId 创建引用。
        /// </summary>
        /// <param name="world">实体所属世界。</param>
        /// <param name="runtimeId">运行时唯一 id。</param>
        public EntityRef(World world, long runtimeId)
            : this(world, runtimeId, null)
        {
        }

        /// <summary>
        /// 从世界、RuntimeId 和捕获实例创建引用。
        /// </summary>
        /// <param name="world">实体所属世界。</param>
        /// <param name="runtimeId">运行时唯一 id。</param>
        /// <param name="entity">捕获时的实体实例。</param>
        internal EntityRef(World world, long runtimeId, T entity)
        {
            m_world = world;
            m_runtimeId = runtimeId;
            m_entity = entity;
        }

        #endregion

        #region Query

        /// <summary>
        /// 尝试解析引用到的实体。
        /// </summary>
        /// <param name="entity">解析到的实体。</param>
        /// <returns>解析成功且未被对象池复用时返回 true。</returns>
        public bool TryGet(out T entity)
        {
            entity = null;
            if (m_world == null || m_runtimeId == 0)
            {
                return false;
            }

            if (m_entity != null && m_entity.RuntimeId != m_runtimeId)
            {
                return false;
            }

            entity = m_world.GetEntity<T>(m_runtimeId);
            return entity != null && entity.RuntimeId == m_runtimeId;
        }

        /// <summary>
        /// 判断指定实体是否与当前引用指向同一个运行时实体。
        /// </summary>
        /// <param name="entity">待比较实体。</param>
        /// <returns>实体属于同一 World 且 RuntimeId 相同时返回 true。</returns>
        public bool IsSameEntity(T entity)
        {
            return entity != null && !entity.IsDisposed && entity.World == m_world && entity.RuntimeId == m_runtimeId;
        }

        #endregion
    }
}
