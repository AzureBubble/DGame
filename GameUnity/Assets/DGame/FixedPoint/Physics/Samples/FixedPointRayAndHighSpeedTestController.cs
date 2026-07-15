#if UNITY_2021_3_OR_NEWER
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DGame.FixedPoint.Samples
{
    /// <summary>
    /// 绘制全部已实现的定点射线测试，并提供高速连续碰撞与高速射线发射器。
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class FixedPointRayAndHighSpeedTestController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private float m_rayLength = 8f;
        [SerializeField] private float m_highSpeedDistance = 25f;
        [SerializeField] private float m_projectileRadius = 0.35f;

        private readonly List<RayDebugResult> m_rayResults = new();
        private readonly StringBuilder m_raySummary = new();
        private MaterialPropertyBlock m_propertyBlock;

        private FPPhysicsContext m_context;
        private FPSphereCollider m_sphereTarget;
        private FPAABBCollider m_aabbTarget;
        private FPBoxCollider m_obbTarget;
        private FPCapsuleCollider m_capsuleTarget;
        private Transform m_planeTarget;
        private Transform m_triangleTarget;
        private Transform m_collisionEmitter;
        private Transform m_collisionProjectile;
        private FPAABBCollider m_collisionWall;
        private Transform m_rayEmitter;
        private Transform m_rayProjectile;
        private FPAABBCollider m_rayWall;

        private bool m_collisionFired;
        private bool m_collisionHit;
        private Vector3 m_collisionPathStart;
        private Vector3 m_collisionPathEnd;
        private string m_collisionStatus = "待发射";

        private bool m_highSpeedRayFired;
        private bool m_highSpeedRayHit;
        private Vector3 m_highSpeedRayStart;
        private Vector3 m_highSpeedRayEnd;
        private Vector3 m_highSpeedRayHitPoint;
        private Vector3 m_highSpeedRayHitNormal;
        private string m_highSpeedRayStatus = "待发射";

        private void Awake()
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            m_context = FPPhysicsPresenter.Instance.context;
            CacheSceneObjects();
            ApplyVisualColors();
            ResetLaunchers();
            RunAllRayTests();
        }

        private void Update()
        {
            // 射线目标允许在 Scene 视图拖动，因此逐帧刷新测试结果。
            RunAllRayTests();
        }

        private void CacheSceneObjects()
        {
            m_sphereTarget = FindCollider<FPSphereCollider>("Sphere_Target");
            m_aabbTarget = FindCollider<FPAABBCollider>("AABB_Target");
            m_obbTarget = FindCollider<FPBoxCollider>("OBB_Target");
            m_capsuleTarget = FindCollider<FPCapsuleCollider>("Capsule_Target");
            m_planeTarget = FindTransform("RayPlane_Target");
            m_triangleTarget = FindTransform("RayTriangle_Target");
            m_collisionEmitter = FindTransform("HighSpeedCollisionEmitter");
            m_collisionProjectile = FindTransform("HighSpeedCollisionProjectile");
            m_collisionWall = FindCollider<FPAABBCollider>("HighSpeedCollisionWall");
            m_rayEmitter = FindTransform("HighSpeedRayEmitter");
            m_rayProjectile = FindTransform("HighSpeedRayProjectile");
            m_rayWall = FindCollider<FPAABBCollider>("HighSpeedRayWall");
        }

        private void ApplyVisualColors()
        {
            SetRendererColor("RayPlane_Target", new Color(0.7f, 0.25f, 0.9f));
            SetRendererColor("RayTriangle_Target", new Color(0.85f, 0.3f, 0.8f));
            SetRendererColor("Collision_Barrel", new Color(0.95f, 0.3f, 0.1f));
            SetRendererColor("HighSpeedCollisionProjectile", new Color(1f, 0.2f, 0.1f));
            SetRendererColor("Ray_Barrel", new Color(0.1f, 0.75f, 1f));
            SetRendererColor("HighSpeedRayProjectile", new Color(0.1f, 0.9f, 1f));
        }

        private void SetRendererColor(string objectName, Color color)
        {
            var target = GameObject.Find(objectName);
            var targetRenderer = target != null ? target.GetComponent<Renderer>() : null;

            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor(BaseColorId, color);
            m_propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(m_propertyBlock);
            m_propertyBlock.Clear();
        }

        private static T FindCollider<T>(string objectName) where T : FPCollider
        {
            var target = GameObject.Find(objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static Transform FindTransform(string objectName)
        {
            var target = GameObject.Find(objectName);
            return target != null ? target.transform : null;
        }

        /// <summary>重新运行当前源码已实现的全部射线形状测试。</summary>
        public void RunAllRayTests()
        {
            m_rayResults.Clear();

            TestSphereRay();
            TestAabbFiniteRay();
            TestAabbSweepRay();
            TestObbRay();
            TestCapsuleRay();
            TestPlaneRay();
            TestTriangleRay();
        }

        private void TestSphereRay()
        {
            if (m_sphereTarget == null)
            {
                return;
            }

            var origin = m_sphereTarget.position - FixedPointVector3.right * (m_rayLength * 0.5f);
            var hit = FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                origin,
                FixedPointVector3.right,
                m_rayLength,
                m_sphereTarget.position,
                m_sphereTarget.scaledRadius,
                out var collision);
            AddRayResult("Sphere", origin, FixedPointVector3.right, m_rayLength, hit, collision);
        }

        private void TestAabbFiniteRay()
        {
            if (m_aabbTarget == null)
            {
                return;
            }

            var origin = m_aabbTarget.position - FixedPointVector3.right * (m_rayLength * 0.5f);
            var hit = FixedPointIntersection.IntersectWithRayAndAABBFixedPointA(
                origin,
                FixedPointVector3.right,
                m_rayLength,
                m_aabbTarget.min,
                m_aabbTarget.max,
                out var collision);
            AddRayResult("AABB Finite", origin, FixedPointVector3.right, m_rayLength, hit, collision);
        }

        private void TestAabbSweepRay()
        {
            if (m_aabbTarget == null)
            {
                return;
            }

            var origin = m_aabbTarget.position - FixedPointVector3.right * (m_rayLength * 0.5f) +
                         FixedPointVector3.up * 0.35f;
            var delta = FixedPointVector3.right * m_rayLength;
            var parameter = FixedPointIntersection.IntersectWithRayAndAABBFixedPoint(
                origin,
                delta,
                m_aabbTarget.min,
                m_aabbTarget.max,
                out var collision);
            AddRayResult(
                "AABB Sweep",
                origin,
                FixedPointVector3.right,
                m_rayLength,
                parameter != FixedPoint64.MaxValue,
                collision);
        }

        private void TestObbRay()
        {
            if (m_obbTarget == null)
            {
                return;
            }

            var origin = m_obbTarget.position - FixedPointVector3.right * (m_rayLength * 0.5f);
            var distance = FixedPointIntersection.IntersectWithRayAndOBBFixedPoint(
                origin,
                FixedPointVector3.right,
                m_rayLength,
                m_obbTarget.position,
                m_obbTarget.halfSize,
                m_obbTarget.fpTransform.fixedPointMatrix,
                out var collision);
            AddRayResult("OBB", origin, FixedPointVector3.right, m_rayLength, distance >= 0, collision);
        }

        private void TestCapsuleRay()
        {
            if (m_capsuleTarget == null)
            {
                return;
            }

            var origin = m_capsuleTarget.position - FixedPointVector3.right * (m_rayLength * 0.5f);
            var collision = FixedPointIntersection.IntersectWithRayAndCapsule(
                origin,
                FixedPointVector3.right,
                m_rayLength,
                m_capsuleTarget.startPos,
                m_capsuleTarget.endPos,
                m_capsuleTarget.scaledRadius);
            AddRayResult("Capsule", origin, FixedPointVector3.right, m_rayLength, collision.hit, collision);
        }

        private void TestPlaneRay()
        {
            if (m_planeTarget == null)
            {
                return;
            }

            var center = new FixedPointVector3(m_planeTarget.position);
            var origin = center - FixedPointVector3.right * (m_rayLength * 0.5f);
            var normal = FixedPointVector3.right;
            var planeDistance = FixedPointVector3.Dot(center, normal);
            var hit = FixedPointIntersection.IntersectWithRayAndPlaneFixedPoint(
                origin,
                FixedPointVector3.right,
                planeDistance,
                normal,
                out var collision) && collision.t <= m_rayLength;
            AddRayResult("Plane", origin, FixedPointVector3.right, m_rayLength, hit, collision);
        }

        private void TestTriangleRay()
        {
            if (m_triangleTarget == null)
            {
                return;
            }

            var center = new FixedPointVector3(m_triangleTarget.position);
            var origin = center - FixedPointVector3.right * (m_rayLength * 0.5f);
            var hit = FixedPointIntersection.IntersectWithRayAndTriangleFixedPoint(
                origin,
                FixedPointVector3.right,
                center,
                new FixedPointVector3(0, -1, -1),
                new FixedPointVector3(0, 1, -1),
                new FixedPointVector3(0, 0, 1),
                out var collision) && collision.t <= m_rayLength;
            AddRayResult("Triangle", origin, FixedPointVector3.right, m_rayLength, hit, collision);
        }

        private void AddRayResult(
            string name,
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            bool hit,
            FPCollision collision)
        {
            var normalizedDirection = direction.normalized;
            m_rayResults.Add(new RayDebugResult
            {
                Name = name,
                Origin = origin.ToVector3(),
                End = (origin + normalizedDirection * length).ToVector3(),
                Hit = hit,
                HitPoint = hit ? collision.closestPoint.ToVector3() : Vector3.zero,
                HitNormal = hit ? collision.normal.ToVector3() : Vector3.zero
            });
        }

        /// <summary>沿完整位移连续扫描高速球体，并停在首次接触位置。</summary>
        public void FireHighSpeedCollision()
        {
            if (m_context == null || m_collisionEmitter == null || m_collisionProjectile == null ||
                m_collisionWall == null)
            {
                return;
            }

            m_collisionPathStart = m_collisionEmitter.position + m_collisionEmitter.right * 1.5f;
            var direction = new FixedPointVector3(m_collisionEmitter.right).normalized;
            m_collisionPathEnd = m_collisionPathStart + direction.ToVector3() * m_highSpeedDistance;
            m_collisionProjectile.gameObject.SetActive(true);
            m_collisionFired = true;
            m_collisionHit = m_context.SphereCast(
                new FixedPointVector3(m_collisionPathStart),
                m_projectileRadius,
                direction,
                m_highSpeedDistance,
                out var hit,
                -1,
                true);

            if (m_collisionHit && hit != null)
            {
                m_collisionProjectile.position =
                    (hit.point + hit.normal * m_projectileRadius).ToVector3();
                m_collisionStatus = hit.fpCollider == m_collisionWall
                    ? $"SphereCast CCD 命中 {m_collisionWall.name}"
                    : $"SphereCast CCD 先命中 {hit.fpCollider.name}";
                return;
            }

            m_collisionProjectile.position = m_collisionPathEnd;
            m_collisionStatus = "SphereCast CCD 未命中";
        }

        /// <summary>沿高速物体完整位移发射八叉树射线，并停在最近命中点。</summary>
        public void FireHighSpeedRaycast()
        {
            if (m_context == null || m_rayEmitter == null || m_rayProjectile == null || m_rayWall == null)
            {
                return;
            }

            m_highSpeedRayStart = m_rayEmitter.position + m_rayEmitter.right * 1.5f;
            var direction = new FixedPointVector3(m_rayEmitter.right).normalized;
            m_highSpeedRayEnd = m_highSpeedRayStart + direction.ToVector3() * m_highSpeedDistance;
            m_rayProjectile.gameObject.SetActive(true);
            m_highSpeedRayFired = true;
            m_highSpeedRayHit = m_context.Raycast(
                new FixedPointVector3(m_highSpeedRayStart),
                direction,
                m_highSpeedDistance,
                out var hit,
                -1,
                true);

            if (m_highSpeedRayHit && hit != null)
            {
                m_highSpeedRayHitPoint = hit.point.ToVector3();
                m_highSpeedRayHitNormal = hit.normal.ToVector3();
                m_rayProjectile.position = m_highSpeedRayHitPoint;
                m_highSpeedRayStatus = hit.fpCollider == m_rayWall
                    ? $"命中 {m_rayWall.name}"
                    : $"先命中 {hit.fpCollider.name}";
            }
            else
            {
                m_highSpeedRayHitPoint = Vector3.zero;
                m_highSpeedRayHitNormal = Vector3.zero;
                m_rayProjectile.position = m_highSpeedRayEnd;
                m_highSpeedRayStatus = "未命中";
            }
        }

        /// <summary>把两个高速测试弹丸恢复到发射口。</summary>
        public void ResetLaunchers()
        {
            m_collisionFired = false;
            m_collisionHit = false;
            m_highSpeedRayFired = false;
            m_highSpeedRayHit = false;
            m_collisionStatus = "待发射";
            m_highSpeedRayStatus = "待发射";

            if (m_collisionEmitter != null && m_collisionProjectile != null)
            {
                m_collisionProjectile.position = m_collisionEmitter.position + m_collisionEmitter.right * 1.5f;
                m_collisionProjectile.gameObject.SetActive(true);
            }

            if (m_rayEmitter != null && m_rayProjectile != null)
            {
                m_rayProjectile.position = m_rayEmitter.position + m_rayEmitter.right * 1.5f;
                m_rayProjectile.gameObject.SetActive(true);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            foreach (var ray in m_rayResults)
            {
                Gizmos.color = ray.Hit ? Color.cyan : Color.red;
                Gizmos.DrawLine(ray.Origin, ray.End);

                if (!ray.Hit)
                {
                    continue;
                }

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(ray.HitPoint, 0.1f);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(ray.HitPoint, ray.HitPoint + ray.HitNormal);
            }

            if (m_collisionFired)
            {
                Gizmos.color = m_collisionHit ? Color.green : Color.red;
                Gizmos.DrawLine(m_collisionPathStart, m_collisionPathEnd);
            }

            if (m_highSpeedRayFired)
            {
                Gizmos.color = m_highSpeedRayHit ? Color.green : Color.red;
                Gizmos.DrawLine(m_highSpeedRayStart, m_highSpeedRayEnd);

                if (m_highSpeedRayHit)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(m_highSpeedRayHitPoint, 0.15f);
                    Gizmos.DrawLine(
                        m_highSpeedRayHitPoint,
                        m_highSpeedRayHitPoint + m_highSpeedRayHitNormal * 1.5f);
                }
            }
        }

        private void OnGUI()
        {
            m_raySummary.Clear();

            foreach (var result in m_rayResults)
            {
                if (m_raySummary.Length > 0)
                {
                    m_raySummary.Append("　");
                }

                m_raySummary.Append(result.Name).Append(result.Hit ? "✓" : "✗");
            }

            GUI.Box(new Rect(16f, 250f, 720f, 205f), GUIContent.none);
            GUI.Label(new Rect(32f, 262f, 690f, 22f), "定点射线与高速物体测试");
            GUI.Label(new Rect(32f, 288f, 690f, 44f), $"全部射线：{m_raySummary}");
            GUI.Label(new Rect(32f, 333f, 690f, 22f), $"高速连续碰撞：{m_collisionStatus}");
            GUI.Label(new Rect(32f, 357f, 690f, 22f), $"高速位移射线：{m_highSpeedRayStatus}");

            if (GUI.Button(new Rect(32f, 392f, 205f, 42f), "发射高速 SphereCast"))
            {
                FireHighSpeedCollision();
            }

            if (GUI.Button(new Rect(249f, 392f, 205f, 42f), "发射高速位移射线"))
            {
                FireHighSpeedRaycast();
            }

            if (GUI.Button(new Rect(466f, 392f, 120f, 42f), "重置"))
            {
                ResetLaunchers();
            }

            if (GUI.Button(new Rect(598f, 392f, 120f, 42f), "重测射线"))
            {
                RunAllRayTests();
            }
        }

        private struct RayDebugResult
        {
            public string Name;
            public Vector3 Origin;
            public Vector3 End;
            public bool Hit;
            public Vector3 HitPoint;
            public Vector3 HitNormal;
        }
    }
}
#endif
