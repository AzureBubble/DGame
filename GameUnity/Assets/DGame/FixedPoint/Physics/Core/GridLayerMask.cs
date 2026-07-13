namespace DGame.FixedPoint
{
    /// <summary>
    /// 封装定点物理系统使用的层掩码，并提供层的添加、移除和匹配操作。
    /// </summary>
    public class GridLayerMask
    {
        /// <summary>
        /// 获取当前层掩码。
        /// </summary>
        public int LayerMask { get; private set; }

        /// <summary>
        /// 使用指定层掩码创建实例。
        /// </summary>
        /// <param name="layerMask">初始层掩码。</param>
        public GridLayerMask(int layerMask)
        {
            LayerMask = layerMask;
        }

        /// <summary>
        /// 将指定层加入当前层掩码。
        /// </summary>
        /// <param name="layer">层索引，有效范围为 0 到 31。</param>
        public void AddLayer(uint layer)
        {
            if (layer > 31)
            {
                return;
            }

            AddLayers(1 << (int)layer);
        }

        /// <summary>
        /// 将指定层掩码包含的所有层加入当前层掩码。
        /// </summary>
        /// <param name="layerMask">需要加入的层掩码。</param>
        public void AddLayers(int layerMask)
        {
            LayerMask = LayerMask | layerMask;
        }

        /// <summary>
        /// 从当前层掩码移除指定层。
        /// </summary>
        /// <param name="layer">层索引，有效范围为 0 到 31。</param>
        public void RemoveLayer(uint layer)
        {
            if (layer > 31)
            {
                return;
            }

            RemoveLayers(1 << (int)layer);
        }

        /// <summary>
        /// 从当前层掩码移除指定层掩码包含的所有层。
        /// </summary>
        /// <param name="layerMask">需要移除的层掩码。</param>
        public void RemoveLayers(int layerMask)
        {
            LayerMask &= ~layerMask;
        }

        /// <summary>
        /// 判断源层掩码与目标层掩码是否匹配。
        /// </summary>
        /// <param name="sourceLayerMask">源层掩码；0 表示不限制层，-1 表示包含全部层。</param>
        /// <param name="layerMask">目标层掩码。</param>
        /// <returns>源层掩码不限制层，或两个掩码至少包含一个相同层时返回 <see langword="true"/>。</returns>
        public static bool ValidateLayerMask(int sourceLayerMask, int layerMask)
        {
            return sourceLayerMask == 0 || (sourceLayerMask & layerMask) != 0;
        }

        /// <summary>
        /// 判断当前层掩码是否包含目标层掩码中的任意层。
        /// </summary>
        /// <param name="layerMask">目标层掩码。</param>
        /// <returns>至少包含一个相同层时返回 <see langword="true"/>。</returns>
        public bool ContainLayer(int layerMask)
        {
            return (LayerMask & layerMask) != 0;
        }

        /// <summary>
        /// 将整数层掩码隐式转换为 <see cref="GridLayerMask"/>。
        /// </summary>
        /// <param name="value">整数层掩码。</param>
        /// <returns>使用指定层掩码创建的实例。</returns>
        public static implicit operator GridLayerMask(int value)
        {
            return new GridLayerMask(value);
        }

        /// <summary>
        /// 将 <see cref="GridLayerMask"/> 隐式转换为整数层掩码。
        /// </summary>
        /// <param name="gridLayerMask">需要转换的层掩码实例。</param>
        /// <returns>实例保存的整数层掩码。</returns>
        public static implicit operator int(GridLayerMask gridLayerMask)
        {
            return gridLayerMask.LayerMask;
        }
    }
}
