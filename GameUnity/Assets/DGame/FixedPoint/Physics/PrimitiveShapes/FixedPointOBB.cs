/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数有向包围盒。
 */
using System;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用中心点、半尺寸和旋转描述的定点数有向包围盒。
    /// </summary>
    public class FixedPointOBB : FixedPointShape
    {
        /// <summary>包围盒中心点。</summary>
        public FixedPointVector3 position;

        /// <summary>包围盒沿三个局部坐标轴方向的半尺寸。</summary>
        public FixedPointVector3 size;

        bool isMatrixDirty;
        bool isQuaternionDirty;
        FixedPointMatrix _orientation;

        /// <summary>
        /// 获取或设置包围盒的旋转矩阵。
        /// </summary>
        /// <remarks>设置矩阵后，四元数会在下次访问时按需更新。</remarks>
        public FixedPointMatrix orientation
        {
            get
            {
                if (isMatrixDirty)
                {
                    _orientation = FixedPointMatrix.CreateFromQuaternion(_quaternion);
                    isMatrixDirty = false;
                }

                return _orientation;
            }
            set
            {
                _orientation = value;
                isMatrixDirty = false;
                isQuaternionDirty = true;
            }
        }

        FixedPointQuaternion _quaternion;

        /// <summary>
        /// 获取或设置包围盒的旋转四元数。
        /// </summary>
        /// <remarks>设置四元数后，旋转矩阵会在下次访问时按需更新。</remarks>
        public FixedPointQuaternion quaternion
        {
            get
            {
                if (isQuaternionDirty)
                {
                    _quaternion = FixedPointQuaternion.CreateFromMatrix(_orientation);
                    isQuaternionDirty = false;
                }

                return _quaternion;
            }
            set
            {
                _quaternion = value;
                isQuaternionDirty = false;
                isMatrixDirty = true;
            }
        }

        /// <summary>
        /// 可选的轴对齐包围盒缓存；当前类不会自动维护该字段。
        /// </summary>
        public FixedPointAABB aabb;

        /// <summary>
        /// 创建位于原点、半尺寸为零且无旋转的有向包围盒。
        /// </summary>
        public FixedPointOBB()
        {
            _orientation = FixedPointMatrix.Identity;
            _quaternion = FixedPointQuaternion.identity;
            shape = ShapeType.OBB;
        }

        /// <summary>
        /// 使用旋转矩阵创建有向包围盒。
        /// </summary>
        /// <param name="position">包围盒中心点。</param>
        /// <param name="size">沿三个局部坐标轴方向的半尺寸。</param>
        /// <param name="orientation">包围盒旋转矩阵。</param>
        /// <exception cref="ArgumentOutOfRangeException">任意半尺寸分量小于零。</exception>
        public FixedPointOBB(FixedPointVector3 position, FixedPointVector3 size, FixedPointMatrix orientation)
        {
            ValidateSize(size);
            this.position = position;
            this.size = size;
            _orientation = orientation;
            _quaternion = FixedPointQuaternion.CreateFromMatrix(orientation);
            shape = ShapeType.OBB;
        }

        /// <summary>
        /// 使用旋转四元数创建有向包围盒。
        /// </summary>
        /// <param name="position">包围盒中心点。</param>
        /// <param name="size">沿三个局部坐标轴方向的半尺寸。</param>
        /// <param name="orientation">包围盒旋转四元数。</param>
        /// <exception cref="ArgumentOutOfRangeException">任意半尺寸分量小于零。</exception>
        public FixedPointOBB(FixedPointVector3 position, FixedPointVector3 size, FixedPointQuaternion orientation)
        {
            ValidateSize(size);
            this.position = position;
            this.size = size;
            _quaternion = orientation;
            _orientation = FixedPointMatrix.CreateFromQuaternion(orientation);
            shape = ShapeType.OBB;
        }

        /// <summary>
        /// 使用欧拉角创建有向包围盒。
        /// </summary>
        /// <param name="position">包围盒中心点。</param>
        /// <param name="size">沿三个局部坐标轴方向的半尺寸。</param>
        /// <param name="orientation">以度为单位的欧拉角。</param>
        /// <exception cref="ArgumentOutOfRangeException">任意半尺寸分量小于零。</exception>
        public FixedPointOBB(FixedPointVector3 position, FixedPointVector3 size, FixedPointVector3 orientation)
        {
            ValidateSize(size);
            this.position = position;
            this.size = size;
            _quaternion = FixedPointQuaternion.Euler(orientation);
            _orientation = FixedPointMatrix.CreateFromQuaternion(_quaternion);
            shape = ShapeType.OBB;
        }

#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            var previousMatrix = Gizmos.matrix;
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.matrix = Matrix4x4.TRS(position.ToVector3(), quaternion.ToQuaternion(), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, (size * 2).ToVector3());
            Gizmos.matrix = previousMatrix;
        }
#endif

        /// <summary>
        /// 判断指定点是否位于有向包围盒内部或边界上。
        /// </summary>
        /// <param name="point">待检测的世界坐标点。</param>
        /// <returns>点位于包围盒内部或边界上时返回 <see langword="true"/>。</returns>
        public bool PointInOBB(FixedPointVector3 point)
        {
            return FixedPointIntersection.PointInOBB(point, position, size, orientation);
        }

        /// <summary>
        /// 验证有向包围盒的半尺寸是否合法。
        /// </summary>
        /// <param name="size">待验证的半尺寸。</param>
        /// <exception cref="ArgumentOutOfRangeException">任意半尺寸分量小于零。</exception>
        private static void ValidateSize(FixedPointVector3 size)
        {
            if (size.x < FixedPoint64.Zero || size.y < FixedPoint64.Zero || size.z < FixedPoint64.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "有向包围盒的半尺寸不能小于零。");
            }
        }
    }
}