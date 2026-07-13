#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点算术表示位置、旋转、缩放的变换。纯逻辑部分双端共用；Unity 序列化/同步部分见 FPTransform.Unity.cs。
    /// </summary>
    public partial class FPTransform
    {
        internal int indexInList;

#if UNITY_2021_3_OR_NEWER
        [SerializeField] [HideInInspector]
#endif
        private FPTransform parent;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPointVector3 m_position;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPointVector3 m_euler;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPointVector3 m_scale;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPointQuaternion m_localRotation = new(0, 0, 0, 1);

        internal bool colliderUpdateFlag;

        /// <summary>
        /// 获取或设置相对于父级变换的局部缩放。
        /// </summary>
        public FixedPointVector3 localScale
        {
            get => m_scale;
            set
            {
                m_scale = value;
                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 获取或设置相对于父级变换的局部位置。
        /// </summary>
        public FixedPointVector3 localPosition
        {
            get => m_position;
            set
            {
                m_position = value;
                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 用于计算带偏移量的位置（如碰撞体中心），比分别计算位置与偏移量更快。
        /// </summary>
        /// <param name="offset">叠加到局部位置上的偏移量。</param>
        /// <returns>叠加偏移量后的世界空间位置。</returns>
        internal FixedPointVector3 Position(FixedPointVector3 offset)
        {
            if (parent == null)
            {
                return m_position + offset;
            }

            return parent.position + parent.rotation * (m_position + offset);
        }

        /// <summary>
        /// 获取或设置世界空间下的位置。
        /// </summary>
        public FixedPointVector3 position
        {
            get
            {
                if (parent == null)
                {
                    return m_position;
                }

                return parent.position + parent.rotation * m_position;
            }
            set
            {
                if (parent == null)
                {
                    m_position = value;
                }
                else
                {
                    var p = value - parent.position;
                    m_position = FixedPointQuaternion.Inverse(parent.rotation) * p;
                }

                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 获取或设置相对于父级变换的局部旋转。
        /// </summary>
        public FixedPointQuaternion localRotation
        {
            get => m_localRotation;
            set
            {
                m_localRotation = value;
                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 获取或设置世界空间下的旋转。
        /// </summary>
        public FixedPointQuaternion rotation
        {
            get
            {
                if (parent == null)
                {
                    return m_localRotation;
                }

                return parent.rotation * m_localRotation;
            }
            set
            {
                if (parent == null)
                {
                    m_localRotation = value;
                }
                else
                {
                    m_localRotation = FixedPointQuaternion.Inverse(parent.rotation) * value;
                }

                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 获取或设置相对于父级变换的局部欧拉角（单位：度）。
        /// </summary>
        public FixedPointVector3 localEulerAngles
        {
            get
            {
                m_euler = m_localRotation.eulerAngles;
                return m_euler;
            }
            set
            {
                m_euler = value;
                m_localRotation = FixedPointQuaternion.Euler(m_euler);
                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 获取或设置世界空间下的欧拉角（单位：度）。
        /// </summary>
        public FixedPointVector3 eulerAngles
        {
            get
            {
                m_euler = m_localRotation.eulerAngles;
                return parent == null ? m_euler : rotation.eulerAngles;
            }
            set
            {
                rotation = FixedPointQuaternion.Euler(value);
                m_euler = m_localRotation.eulerAngles;
                colliderUpdateFlag = true;
            }
        }

        /// <summary>
        /// 将变换按指定欧拉角叠加旋转（作用于局部旋转）。
        /// </summary>
        /// <param name="euler">旋转增量的欧拉角（单位：度）。</param>
        public void Rotate(FixedPointVector3 euler)
        {
            localRotation = m_localRotation * FixedPointQuaternion.Euler(euler);
        }

        /// <summary>
        /// 获取世界空间下的缩放（与父级缩放逐分量相乘）。
        /// </summary>
        public FixedPointVector3 scale =>
            parent == null ? localScale : FixedPointVector3.Scale(localScale, parent.scale);

        /// <summary>
        /// 获取由当前世界旋转构造的定点旋转矩阵。
        /// </summary>
        public FixedPointMatrix fixedPointMatrix => FixedPointMatrix.CreateFromQuaternion(rotation);

        /// <summary>
        /// 获取世界空间下的前方向量（+Z）。
        /// </summary>
        public FixedPointVector3 forward => rotation * FixedPointVector3.forward;

        /// <summary>
        /// 获取世界空间下的后方向量（-Z）。
        /// </summary>
        public FixedPointVector3 back => rotation * FixedPointVector3.back;

        /// <summary>
        /// 获取世界空间下的上方向量（+Y）。
        /// </summary>
        public FixedPointVector3 up => rotation * FixedPointVector3.up;

        /// <summary>
        /// 获取世界空间下的下方向量（-Y）。
        /// </summary>
        public FixedPointVector3 down => rotation * FixedPointVector3.down;

        /// <summary>
        /// 获取世界空间下的右方向量（+X）。
        /// </summary>
        public FixedPointVector3 right => rotation * FixedPointVector3.right;

        /// <summary>
        /// 获取世界空间下的左方向量（-X）。
        /// </summary>
        public FixedPointVector3 left => rotation * FixedPointVector3.left;
    }
}