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
        [SerializeField] private float m_visualCaseDuration = 2.5f;

        private readonly List<RayDebugResult> m_rayResults = new();
        private readonly List<RegressionResult> m_regressionResults = new();
        private List<FPCollision> m_overlapCollisions = new();
        private readonly List<FPRaycastHit> m_allQueryHits = new();
        private readonly List<int> m_visualMeshCandidates = new();
        private readonly FPCollider[] m_nonAllocColliders = new FPCollider[32];
        private readonly FPRaycastHit[] m_nonAllocQueryHits = new FPRaycastHit[32];
        private readonly StringBuilder m_raySummary = new();
        private readonly StringBuilder m_regressionSummary = new();
        private Vector2 m_regressionScroll;
        private MaterialPropertyBlock m_propertyBlock;

        private FPPhysicsContext m_context;
        private FPSphereCollider m_sphereTarget;
        private FPAABBCollider m_aabbTarget;
        private FPBoxCollider m_obbTarget;
        private FPCapsuleCollider m_capsuleTarget;
        private FPAACapsuleCollider m_aaCapsuleTarget;
        private FPCylinderCollider m_cylinderTarget;
        private FPMeshCollider m_meshTarget;
        private FPCharacterController m_characterTarget;
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

        private const int VisualCaseCount = 15;
        private Transform m_visualRoot;
        private Transform m_visualSphere;
        private Transform m_visualBox;
        private Transform m_visualCapsule;
        private Transform m_visualCylinder;
        private Transform m_visualActive;
        private Renderer m_visualRenderer;
        private float m_visualSequenceStart;
        private int m_visualCaseIndex = -1;
        private bool m_visualCaseObservedHit;
        private bool m_visualHit;
        private float m_visualProgress;
        private string m_visualCaseName = "初始化";

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
            RunRegressionTests();
            CreateVisualProbes();
            m_visualSequenceStart = Time.time;
        }

        private void Update()
        {
            // 射线目标允许在 Scene 视图拖动，因此逐帧刷新测试结果。
            RunAllRayTests();
            UpdateVisualRegression();
        }

        private void OnDestroy()
        {
            if (m_visualRoot != null)
            {
                Destroy(m_visualRoot.gameObject);
            }
        }

        private void CacheSceneObjects()
        {
            m_sphereTarget = FindCollider<FPSphereCollider>("Sphere_Target");
            m_aabbTarget = FindCollider<FPAABBCollider>("AABB_Target");
            m_obbTarget = FindCollider<FPBoxCollider>("OBB_Target");
            m_capsuleTarget = FindCollider<FPCapsuleCollider>("Capsule_Target");
            m_aaCapsuleTarget = FindCollider<FPAACapsuleCollider>("AACapsule_Target");
            m_cylinderTarget = FindCollider<FPCylinderCollider>("Cylinder_Target");
            m_meshTarget = FindCollider<FPMeshCollider>("Mesh_Target");
            m_characterTarget = FindCollider<FPCharacterController>("CharacterController_Target");
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

        private void CreateVisualProbes()
        {
            var root = new GameObject("Runtime_VisualRegression");
            root.transform.SetParent(transform, false);
            m_visualRoot = root.transform;
            m_visualSphere = CreateVisualPrimitive(PrimitiveType.Sphere, "Visual_Sphere");
            m_visualBox = CreateVisualPrimitive(PrimitiveType.Cube, "Visual_Box");
            m_visualCapsule = CreateVisualPrimitive(PrimitiveType.Capsule, "Visual_Capsule");
            m_visualCylinder = CreateVisualPrimitive(PrimitiveType.Cylinder, "Visual_Cylinder");
            SetVisualShape(m_visualSphere, Vector3.one * 0.5f, Quaternion.identity);
        }

        private Transform CreateVisualPrimitive(PrimitiveType primitiveType, string objectName)
        {
            var visual = GameObject.CreatePrimitive(primitiveType);
            visual.name = objectName;
            visual.layer = 2;
            visual.transform.SetParent(m_visualRoot, false);
            var unityCollider = visual.GetComponent<Collider>();
            if (unityCollider != null) Destroy(unityCollider);
            return visual.transform;
        }

        private void SetVisualShape(Transform activeShape, Vector3 scale, Quaternion rotation)
        {
            m_visualSphere.gameObject.SetActive(activeShape == m_visualSphere);
            m_visualBox.gameObject.SetActive(activeShape == m_visualBox);
            m_visualCapsule.gameObject.SetActive(activeShape == m_visualCapsule);
            m_visualCylinder.gameObject.SetActive(activeShape == m_visualCylinder);
            activeShape.localScale = scale;
            activeShape.rotation = rotation;
            m_visualActive = activeShape;
            m_visualRenderer = activeShape.GetComponent<Renderer>();
        }

        private void UpdateVisualRegression()
        {
            if (m_visualRoot == null || m_context == null) return;
            var elapsed = Time.time - m_visualSequenceStart;
            var sequencePosition = elapsed / Mathf.Max(0.5f, m_visualCaseDuration);
            var caseIndex = Mathf.FloorToInt(sequencePosition) % VisualCaseCount;
            var progress = sequencePosition - Mathf.Floor(sequencePosition);
            m_visualProgress = Mathf.SmoothStep(0f, 1f, progress);
            if (caseIndex != m_visualCaseIndex)
            {
                m_visualCaseIndex = caseIndex;
                m_visualCaseObservedHit = false;
                ConfigureVisualCase(caseIndex);
                Debug.Log($"[FixedPointPhysicsVisual] 开始 {m_visualCaseName}", this);
            }

            EvaluateVisualCase(caseIndex, m_visualProgress);
            var color = m_visualHit
                ? new Color(0.15f, 0.95f, 0.35f)
                : m_visualProgress > 0.92f
                    ? new Color(1f, 0.2f, 0.15f)
                    : new Color(0.1f, 0.85f, 1f);
            SetVisualRendererColor(color);
            if (m_visualHit && !m_visualCaseObservedHit)
            {
                m_visualCaseObservedHit = true;
                Debug.Log($"[FixedPointPhysicsVisual] 命中 {m_visualCaseName}", this);
            }
        }

        private void ConfigureVisualCase(int caseIndex)
        {
            switch (caseIndex)
            {
                case 0:
                    m_visualCaseName = "Ray ↔ Cylinder";
                    SetVisualShape(m_visualSphere, Vector3.one * 0.24f, Quaternion.identity);
                    break;
                case 1:
                    m_visualCaseName = "Ray ↔ Mesh BVH";
                    SetVisualShape(m_visualSphere, Vector3.one * 0.24f, Quaternion.identity);
                    break;
                case 2:
                    m_visualCaseName = "SphereCast ↔ AABB";
                    SetVisualShape(m_visualSphere, Vector3.one * 0.5f, Quaternion.identity);
                    break;
                case 3:
                    m_visualCaseName = "SphereCast ↔ Cylinder";
                    SetVisualShape(m_visualSphere, Vector3.one * 0.5f, Quaternion.identity);
                    break;
                case 4:
                    m_visualCaseName = "SphereCast ↔ Mesh BVH";
                    SetVisualShape(m_visualSphere, Vector3.one * 0.5f, Quaternion.identity);
                    break;
                case 5:
                    m_visualCaseName = "AABB ↔ AABB 接触";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f, Quaternion.identity);
                    break;
                case 6:
                    m_visualCaseName = "AABB ↔ OBB 接触";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f, Quaternion.identity);
                    break;
                case 7:
                    m_visualCaseName = "OBB ↔ OBB 接触";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f,
                        Quaternion.Euler(12f, 32f, 18f));
                    break;
                case 8:
                    m_visualCaseName = "AABB ↔ Cylinder";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f, Quaternion.identity);
                    break;
                case 9:
                    m_visualCaseName = "OBB ↔ Cylinder";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f,
                        Quaternion.Euler(12f, 32f, 18f));
                    break;
                case 10:
                    m_visualCaseName = "Cylinder ↔ Cylinder";
                    SetVisualShape(m_visualCylinder, new Vector3(1f, 0.9f, 1f), Quaternion.identity);
                    break;
                case 11:
                    m_visualCaseName = "Box ↔ Mesh BVH";
                    SetVisualShape(m_visualBox, Vector3.one, Quaternion.identity);
                    break;
                case 12:
                    m_visualCaseName = "Capsule/Character ↔ Mesh BVH";
                    SetVisualShape(m_visualCapsule, new Vector3(0.7f, 0.8f, 0.7f), Quaternion.identity);
                    break;
                case 13:
                    m_visualCaseName = "Cylinder ↔ Mesh BVH";
                    SetVisualShape(m_visualCylinder, new Vector3(0.9f, 0.8f, 0.9f), Quaternion.identity);
                    break;
                default:
                    m_visualCaseName = "OverlayBox ↔ Character";
                    SetVisualShape(m_visualBox, Vector3.one * 1.2f, Quaternion.identity);
                    break;
            }
        }

        private void EvaluateVisualCase(int caseIndex, float progress)
        {
            switch (caseIndex)
            {
                case 0:
                    EvaluateRayCylinderVisual(progress);
                    break;
                case 1:
                    EvaluateRayMeshVisual(progress);
                    break;
                case 2:
                    EvaluateSphereCastAabbVisual(progress);
                    break;
                case 3:
                    EvaluateSphereCastCylinderVisual(progress);
                    break;
                case 4:
                    EvaluateSphereCastMeshVisual(progress);
                    break;
                case 5:
                    EvaluateAabbAabbVisual(progress);
                    break;
                case 6:
                    EvaluateAabbObbVisual(progress);
                    break;
                case 7:
                    EvaluateObbObbVisual(progress);
                    break;
                case 8:
                    EvaluateAabbCylinderVisual(progress);
                    break;
                case 9:
                    EvaluateObbCylinderVisual(progress);
                    break;
                case 10:
                    EvaluateCylinderCylinderVisual(progress);
                    break;
                case 11:
                    EvaluateBoxMeshVisual(progress);
                    break;
                case 12:
                    EvaluateCapsuleMeshVisual(progress);
                    break;
                case 13:
                    EvaluateCylinderMeshVisual(progress);
                    break;
                default:
                    EvaluateOverlayBoxCharacterVisual(progress);
                    break;
            }
        }

        private void EvaluateRayCylinderVisual(float progress)
        {
            if (m_cylinderTarget == null) return;
            var origin = m_cylinderTarget.position - FixedPointVector3.right *
                (m_cylinderTarget.scaledRadius + 3);
            const int length = 6;
            var travelled = (FixedPoint64)(length * progress);
            SetVisualPosition(origin + FixedPointVector3.right * travelled);
            var collision = FixedPointIntersection.IntersectWithRayAndCylinder(
                origin, FixedPointVector3.right, length,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);
            m_visualHit = collision.hit && travelled >= collision.t;
        }

        private void EvaluateRayMeshVisual(float progress)
        {
            if (m_meshTarget == null) return;
            var origin = m_meshTarget.position + FixedPointVector3.up * 5;
            const int length = 8;
            var travelled = (FixedPoint64)(length * progress);
            SetVisualPosition(origin + FixedPointVector3.down * travelled);
            var collision = IntersectVisualRayWithMesh(
                origin, FixedPointVector3.down, length, m_meshTarget);
            m_visualHit = collision.hit && travelled >= collision.t;
        }

        private void EvaluateSphereCastAabbVisual(float progress)
        {
            if (m_aabbTarget == null) return;
            var radius = (FixedPoint64)0.25;
            var origin = m_aabbTarget.position - FixedPointVector3.right *
                (m_aabbTarget.halfSize.x + radius + 3);
            const int length = 7;
            var travelled = (FixedPoint64)(length * progress);
            SetVisualPosition(origin + FixedPointVector3.right * travelled);
            var collision = FixedPointIntersection.IntersectWithRayAndRoundedAABB(
                origin, FixedPointVector3.right, length,
                m_aabbTarget.min, m_aabbTarget.max, radius);
            m_visualHit = collision.hit && travelled >= collision.t;
        }

        private void EvaluateSphereCastCylinderVisual(float progress)
        {
            if (m_cylinderTarget == null) return;
            var radius = (FixedPoint64)0.25;
            var origin = m_cylinderTarget.position - FixedPointVector3.right *
                (m_cylinderTarget.scaledRadius + radius + 3);
            const int length = 7;
            var travelled = (FixedPoint64)(length * progress);
            SetVisualPosition(origin + FixedPointVector3.right * travelled);
            var collision = FixedPointIntersection.IntersectWithSphereCastAndCylinder(
                origin, FixedPointVector3.right, length, radius,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);
            m_visualHit = collision.hit && travelled >= collision.t;
        }

        private void EvaluateSphereCastMeshVisual(float progress)
        {
            if (m_meshTarget == null) return;
            var radius = (FixedPoint64)0.25;
            var origin = m_meshTarget.position + FixedPointVector3.up * 5;
            const int length = 8;
            var travelled = (FixedPoint64)(length * progress);
            SetVisualPosition(origin + FixedPointVector3.down * travelled);
            var collision = IntersectVisualSphereCastWithMesh(
                origin, FixedPointVector3.down, length, radius, m_meshTarget);
            m_visualHit = collision.hit && travelled >= collision.t;
        }

        private void EvaluateAabbAabbVisual(float progress)
        {
            if (m_aabbTarget == null) return;
            var center = LerpFixed(
                m_aabbTarget.position - FixedPointVector3.right * 4,
                m_aabbTarget.position,
                progress);
            var halfSize = FixedPointVector3.one * (FixedPoint64)0.6;
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithAABBAndAABBCollision(
                center - halfSize, center + halfSize,
                m_aabbTarget.min, m_aabbTarget.max).hit;
        }

        private void EvaluateAabbObbVisual(float progress)
        {
            if (m_obbTarget == null) return;
            var center = LerpFixed(
                m_obbTarget.position - FixedPointVector3.right * 4,
                m_obbTarget.position,
                progress);
            var halfSize = FixedPointVector3.one * (FixedPoint64)0.6;
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                center - halfSize, center + halfSize,
                m_obbTarget.position, m_obbTarget.halfSize,
                m_obbTarget.fpTransform.fixedPointMatrix).hit;
        }

        private void EvaluateObbObbVisual(float progress)
        {
            if (m_obbTarget == null) return;
            var center = LerpFixed(
                m_obbTarget.position - FixedPointVector3.right * 4,
                m_obbTarget.position,
                progress);
            SetVisualPosition(center);
            var orientation = FixedPointMatrix.CreateFromQuaternion(
                new FixedPointQuaternion(m_visualActive.rotation));
            m_visualHit = FixedPointIntersection.IntersectWithOBBAndOBBCollision(
                center, FixedPointVector3.one * (FixedPoint64)0.6, orientation,
                m_obbTarget.position, m_obbTarget.halfSize,
                m_obbTarget.fpTransform.fixedPointMatrix).hit;
        }

        private void EvaluateAabbCylinderVisual(float progress)
        {
            if (m_cylinderTarget == null) return;
            var center = LerpFixed(
                m_cylinderTarget.position - FixedPointVector3.right * 4,
                m_cylinderTarget.position,
                progress);
            var halfSize = FixedPointVector3.one * (FixedPoint64)0.6;
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithAABBAndCylinder(
                center - halfSize, center + halfSize,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius).hit;
        }

        private void EvaluateObbCylinderVisual(float progress)
        {
            if (m_cylinderTarget == null) return;
            var center = LerpFixed(
                m_cylinderTarget.position - FixedPointVector3.right * 4,
                m_cylinderTarget.position,
                progress);
            SetVisualPosition(center);
            var orientation = FixedPointMatrix.CreateFromQuaternion(
                new FixedPointQuaternion(m_visualActive.rotation));
            m_visualHit = FixedPointIntersection.IntersectWithOBBAndCylinder(
                center, FixedPointVector3.one * (FixedPoint64)0.6, orientation,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius).hit;
        }

        private void EvaluateCylinderCylinderVisual(float progress)
        {
            if (m_cylinderTarget == null) return;
            var center = LerpFixed(
                m_cylinderTarget.position - FixedPointVector3.right * 4,
                m_cylinderTarget.position,
                progress);
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithCylinderAndCylinder(
                center - FixedPointVector3.up * (FixedPoint64)0.9,
                center + FixedPointVector3.up * (FixedPoint64)0.9,
                (FixedPoint64)0.5,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius).hit;
        }

        private void EvaluateBoxMeshVisual(float progress)
        {
            if (m_meshTarget == null) return;
            var center = LerpFixed(
                m_meshTarget.position + FixedPointVector3.up * 5,
                m_meshTarget.position + FixedPointVector3.up * (FixedPoint64)0.3,
                progress);
            var halfSize = FixedPointVector3.one * FixedPoint64.Half;
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithOBBAndMesh(
                center, halfSize, FixedPointMatrix.Identity,
                center - halfSize, center + halfSize,
                m_meshTarget, m_visualMeshCandidates).hit;
        }

        private void EvaluateCapsuleMeshVisual(float progress)
        {
            if (m_meshTarget == null) return;
            var center = LerpFixed(
                m_meshTarget.position + FixedPointVector3.up * 5,
                m_meshTarget.position + FixedPointVector3.up * (FixedPoint64)0.5,
                progress);
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                center - FixedPointVector3.up * (FixedPoint64)0.75,
                center + FixedPointVector3.up * (FixedPoint64)0.75,
                (FixedPoint64)0.35,
                m_meshTarget, m_visualMeshCandidates).hit;
        }

        private void EvaluateCylinderMeshVisual(float progress)
        {
            if (m_meshTarget == null) return;
            var center = LerpFixed(
                m_meshTarget.position + FixedPointVector3.up * 5,
                m_meshTarget.position + FixedPointVector3.up * (FixedPoint64)0.5,
                progress);
            SetVisualPosition(center);
            m_visualHit = FixedPointIntersection.IntersectWithCylinderAndMesh(
                center - FixedPointVector3.up * (FixedPoint64)0.8,
                center + FixedPointVector3.up * (FixedPoint64)0.8,
                (FixedPoint64)0.45,
                m_meshTarget, m_visualMeshCandidates).hit;
        }

        private void EvaluateOverlayBoxCharacterVisual(float progress)
        {
            if (m_characterTarget == null) return;
            var center = LerpFixed(
                m_characterTarget.position - FixedPointVector3.right * 4,
                m_characterTarget.position,
                progress);
            SetVisualPosition(center);
            m_context.OverlayBoxCollision(
                center, FixedPointVector3.one * (FixedPoint64)0.6,
                FixedPointMatrix.Identity, ref m_overlapCollisions, -1, true);
            m_visualHit = ContainsCollider(m_overlapCollisions, m_characterTarget);
        }

        private FPCollision IntersectVisualRayWithMesh(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FPMeshCollider mesh)
        {
            mesh.CollectTriangleCandidates(
                FixedPointVector3.Min(origin, origin + direction * length),
                FixedPointVector3.Max(origin, origin + direction * length),
                m_visualMeshCandidates);
            var best = new FPCollision();
            var bestDistance = FixedPoint64.MaxValue;
            foreach (var triangleIndex in m_visualMeshCandidates)
            {
                mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                if (FixedPointIntersection.IntersectWithRayAndTriangleFixedPoint(
                        origin, direction, FixedPointVector3.zero,
                        a, b, c, out var collision) && collision.t <= length &&
                    collision.t < bestDistance)
                {
                    bestDistance = collision.t;
                    best = collision;
                }
            }

            return best;
        }

        private FPCollision IntersectVisualSphereCastWithMesh(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPoint64 radius,
            FPMeshCollider mesh)
        {
            var radiusVector = FixedPointVector3.one * radius;
            mesh.CollectTriangleCandidates(
                FixedPointVector3.Min(origin, origin + direction * length) - radiusVector,
                FixedPointVector3.Max(origin, origin + direction * length) + radiusVector,
                m_visualMeshCandidates);
            var best = new FPCollision();
            var bestDistance = FixedPoint64.MaxValue;
            foreach (var triangleIndex in m_visualMeshCandidates)
            {
                mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                var collision = FixedPointIntersection.IntersectWithSphereCastAndTriangle(
                    origin, direction, length, radius, a, b, c);
                if (collision.hit && collision.t < bestDistance)
                {
                    bestDistance = collision.t;
                    best = collision;
                }
            }

            return best;
        }

        private void SetVisualPosition(FixedPointVector3 position)
        {
            m_visualActive.position = position.ToVector3();
        }

        private void SetVisualRendererColor(Color color)
        {
            if (m_visualRenderer == null) return;
            m_visualRenderer.GetPropertyBlock(m_propertyBlock);
            m_propertyBlock.SetColor(BaseColorId, color);
            m_propertyBlock.SetColor(ColorId, color);
            m_visualRenderer.SetPropertyBlock(m_propertyBlock);
            m_propertyBlock.Clear();
        }

        private static FixedPointVector3 LerpFixed(
            FixedPointVector3 start,
            FixedPointVector3 end,
            float progress)
        {
            return start + (end - start) * (FixedPoint64)progress;
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

        /// <summary>运行场景内公共查询和边界语义回归检查。</summary>
        public void RunRegressionTests()
        {
            m_regressionResults.Clear();

            TestRaycastBoundarySemantics();
            TestAllQueryRegression();
            TestCylinderRaycastRegression();
            TestCylinderMatrixRegression();
            TestMeshRaycastRegression();
            TestSphereCastRegression();
            TestCylinderSphereCastRegression();
            TestMeshSphereCastRegression();
            TestOverlapRegression();
            TestColliderOverlayRegression();
            TestNegativeScaleRegression();
            TestHotPathAllocationRegression();
            LogRegressionResults();
        }

        private void TestHotPathAllocationRegression()
        {
            if (m_context == null || m_aabbTarget == null || m_cylinderTarget == null ||
                m_meshTarget == null)
            {
                AddRegressionResult("高性能热路径零 GC", false, "场景目标缺失");
                return;
            }

            RunHotPathProbe();
            RunHotPathProbe();
            var allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++)
            {
                RunHotPathProbe();
            }

            var allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            AddRegressionResult("高性能热路径零 GC", allocatedBytes == 0,
                $"64 轮分配={allocatedBytes} bytes");
        }

        private void RunHotPathProbe()
        {
            var halfSize = FixedPointVector3.one * (FixedPoint64)0.6;
            var center = m_cylinderTarget.position;
            FixedPointIntersection.IntersectWithAABBAndCylinder(
                center - halfSize, center + halfSize,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);
            FixedPointIntersection.IntersectWithOBBAndCylinder(
                center, halfSize, FixedPointMatrix.Identity,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);
            FixedPointIntersection.IntersectWithCylinderAndCylinder(
                center - FixedPointVector3.up, center + FixedPointVector3.up,
                FixedPoint64.Half,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);

            var meshCenter = m_meshTarget.position + FixedPointVector3.up * FixedPoint64.Half;
            FixedPointIntersection.IntersectWithOBBAndMesh(
                meshCenter, halfSize, FixedPointMatrix.Identity,
                meshCenter - halfSize, meshCenter + halfSize,
                m_meshTarget, m_visualMeshCandidates);
            FixedPointIntersection.IntersectWithCapsuleAndMesh(
                meshCenter - FixedPointVector3.up,
                meshCenter + FixedPointVector3.up,
                (FixedPoint64)0.35,
                m_meshTarget, m_visualMeshCandidates);
            FixedPointIntersection.IntersectWithCylinderAndMesh(
                meshCenter - FixedPointVector3.up,
                meshCenter + FixedPointVector3.up,
                (FixedPoint64)0.45,
                m_meshTarget, m_visualMeshCandidates);

            var rayOrigin = m_aabbTarget.position - FixedPointVector3.right * 4;
            m_context.RaycastNonAlloc(
                rayOrigin, FixedPointVector3.right, 8,
                m_nonAllocQueryHits, -1, true);
            m_context.SphereCastNonAlloc(
                rayOrigin, (FixedPoint64)0.25, FixedPointVector3.right, 8,
                m_nonAllocQueryHits, -1, true);
            m_context.OverlayBoxCollision(
                m_aabbTarget.position, halfSize, FixedPointMatrix.Identity,
                ref m_overlapCollisions, -1, true);
            m_context.OverlayColliderCollision(
                m_cylinderTarget, ref m_overlapCollisions, -1, true);
        }

        private void LogRegressionResults()
        {
            var passedCount = 0;
            foreach (var result in m_regressionResults)
            {
                if (result.Passed)
                {
                    passedCount++;
                    continue;
                }

                Debug.LogError($"[FixedPointPhysicsTest] {result.Name}: {result.Detail}", this);
            }

            Debug.Log($"[FixedPointPhysicsTest] Regression {passedCount}/{m_regressionResults.Count}", this);
        }

        private void TestRaycastBoundarySemantics()
        {
            if (m_sphereTarget == null)
            {
                AddRegressionResult("Ray/Sphere 目标存在", false, "缺少 Sphere_Target");
                return;
            }

            var center = m_sphereTarget.position;
            var radius = m_sphereTarget.scaledRadius;
            var surface = center + FixedPointVector3.right * radius;
            var tangentOrigin = center + FixedPointVector3.up * radius - FixedPointVector3.right * 2;

            var insideHit = FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                center, FixedPointVector3.right, 4, center, radius, out _);
            AddRegressionResult("Ray 内部起点不命中", !insideHit, insideHit ? "期望 false" : null);

            var surfaceOutHit = FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                surface, FixedPointVector3.right, 0, center, radius, out var surfaceOutCollision);
            AddRegressionResult("Ray 表面向外 t=0", surfaceOutHit && surfaceOutCollision.t == FixedPoint64.Zero,
                surfaceOutHit ? $"t={surfaceOutCollision.t}" : "未命中");

            var surfaceInHit = FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                surface, FixedPointVector3.left, 0, center, radius, out var surfaceInCollision);
            AddRegressionResult("Ray 表面向内 t=0", surfaceInHit && surfaceInCollision.t == FixedPoint64.Zero,
                surfaceInHit ? $"t={surfaceInCollision.t}" : "未命中");

            var tangentHit = FixedPointIntersection.IntersetWithRayAndSphereFixedPoint(
                tangentOrigin, FixedPointVector3.right, 4, center, radius, out var tangentCollision);
            AddRegressionResult("Ray 相切命中", tangentHit && tangentCollision.hit,
                tangentHit ? null : "未命中");

            var publicOrigin = center - FixedPointVector3.right * (radius + 2);
            var publicHit = m_context.Raycast(publicOrigin, FixedPointVector3.right, radius + 4,
                out var hit, -1, true);
            AddRegressionResult("公共 Raycast 命中 Sphere", publicHit && hit?.fpCollider == m_sphereTarget,
                publicHit && hit != null ? $"实际={hit.fpCollider.name}" : "未命中");
            AddRegressionResult("RaycastHit point/normal", publicHit && hit != null &&
                !hit.normal.IsZero() && hit.point == center - FixedPointVector3.right * radius,
                publicHit && hit != null ? $"point={hit.point}, normal={hit.normal}" : "无命中信息");
            AddRegressionResult("RaycastHit distance/outPoint", publicHit && hit != null &&
                hit.distance == 2 && hit.outPoint == center + FixedPointVector3.right * radius,
                publicHit && hit != null ? $"distance={hit.distance}, outPoint={hit.outPoint}" : "无命中信息");
        }

        private void TestSphereCastRegression()
        {
            if (m_aabbTarget == null)
            {
                AddRegressionResult("SphereCast 目标存在", false, "缺少 AABB_Target");
                return;
            }

            var castRadius = (FixedPoint64)0.25;
            var origin = m_aabbTarget.position - FixedPointVector3.right *
                (m_aabbTarget.halfSize.x + castRadius + 2);
            var originalLayer = m_aabbTarget.layer;
            bool hit;
            FPRaycastHit result;
            try
            {
                m_aabbTarget.layer = 31;
                hit = m_context.SphereCast(origin, castRadius, FixedPointVector3.right, 5,
                    out result, 1 << 31, true);
            }
            finally
            {
                m_aabbTarget.layer = originalLayer;
            }
            AddRegressionResult("SphereCast 命中 AABB", hit && result?.fpCollider == m_aabbTarget,
                hit && result != null ? $"实际={result.fpCollider.name}" : "未命中");
            AddRegressionResult("SphereCast point/normal", hit && result != null &&
                !result.normal.IsZero() && result.point == m_aabbTarget.position -
                FixedPointVector3.right * m_aabbTarget.halfSize.x,
                hit && result != null ? $"point={result.point}, normal={result.normal}" : "无命中信息");
        }

        private void TestAllQueryRegression()
        {
            if (m_sphereTarget == null || m_aabbTarget == null)
            {
                AddRegressionResult("All/NonAlloc 目标存在", false, "缺少 Sphere_Target 或 AABB_Target");
                return;
            }

            var rayOrigin = m_sphereTarget.position - FixedPointVector3.right *
                (m_sphereTarget.scaledRadius + 2);
            var allCount = m_context.RaycastAll(
                rayOrigin, FixedPointVector3.right, 6, m_allQueryHits, -1, true);
            AddRegressionResult("RaycastAll 完整集合与排序",
                allCount == m_allQueryHits.Count &&
                ContainsCollider(m_allQueryHits, m_sphereTarget) && IsSorted(m_allQueryHits),
                $"结果数={allCount}");

            var nonAllocCount = m_context.RaycastNonAlloc(
                rayOrigin, FixedPointVector3.right, 6, m_nonAllocQueryHits, -1, true);
            AddRegressionResult("RaycastNonAlloc",
                ContainsCollider(m_nonAllocQueryHits, nonAllocCount, m_sphereTarget) &&
                IsSorted(m_nonAllocQueryHits, nonAllocCount),
                $"结果数={nonAllocCount}");

            var sphereCastOrigin = m_aabbTarget.position - FixedPointVector3.right *
                (m_aabbTarget.halfSize.x + 2);
            var sphereCastAllCount = m_context.SphereCastAll(
                sphereCastOrigin, (FixedPoint64)0.25, FixedPointVector3.right, 6,
                m_allQueryHits, -1, true);
            AddRegressionResult("SphereCastAll 完整集合与排序",
                sphereCastAllCount == m_allQueryHits.Count &&
                ContainsCollider(m_allQueryHits, m_aabbTarget) && IsSorted(m_allQueryHits),
                $"结果数={sphereCastAllCount}");

            var sphereCastNonAllocCount = m_context.SphereCastNonAlloc(
                sphereCastOrigin, (FixedPoint64)0.25, FixedPointVector3.right, 6,
                m_nonAllocQueryHits, -1, true);
            AddRegressionResult("SphereCastNonAlloc",
                ContainsCollider(m_nonAllocQueryHits, sphereCastNonAllocCount, m_aabbTarget) &&
                IsSorted(m_nonAllocQueryHits, sphereCastNonAllocCount),
                $"结果数={sphereCastNonAllocCount}");
        }

        private void TestCylinderRaycastRegression()
        {
            if (m_cylinderTarget == null)
            {
                AddRegressionResult("Ray/Cylinder 目标存在", false, "缺少 Cylinder_Target");
                return;
            }

            var origin = m_cylinderTarget.position - FixedPointVector3.right *
                (m_cylinderTarget.scaledRadius + 2);
            var directCollision = FixedPointIntersection.IntersectWithRayAndCylinder(
                origin, FixedPointVector3.right, 5,
                m_cylinderTarget.startPos, m_cylinderTarget.endPos,
                m_cylinderTarget.scaledRadius);
            AddRegressionResult("Ray/Cylinder 直接算法", directCollision.hit &&
                directCollision.t >= FixedPoint64.Zero && !directCollision.normal.IsZero(),
                directCollision.hit ? $"t={directCollision.t}, normal={directCollision.normal}" : "未命中");

            var publicHit = m_context.Raycast(
                origin, FixedPointVector3.right, 5, out var hit, -1, true);
            AddRegressionResult("公共 Raycast/Cylinder",
                publicHit && hit?.fpCollider == m_cylinderTarget &&
                hit.distance >= FixedPoint64.Zero && !hit.normal.IsZero(),
                publicHit && hit != null ? $"实际={hit.fpCollider.name}, distance={hit.distance}" : "未命中");
        }

        private void TestMeshRaycastRegression()
        {
            if (m_meshTarget == null)
            {
                AddRegressionResult("公共 Raycast/Mesh", false, "缺少 Mesh_Target");
                return;
            }

            var origin = m_meshTarget.position + FixedPointVector3.up * 4;
            var hit = m_context.Raycast(origin, FixedPointVector3.down, 8,
                out var result, -1, true);
            AddRegressionResult("公共 Raycast/Mesh",
                hit && result?.fpCollider == m_meshTarget &&
                result.distance >= FixedPoint64.Zero && !result.normal.IsZero(),
                hit && result != null ? $"实际={result.fpCollider.name}, distance={result.distance}" : "未命中");
        }

        private void TestCylinderMatrixRegression()
        {
            var boxMin = new FixedPointVector3(-1, -1, -1);
            var boxMax = new FixedPointVector3(1, 1, 1);
            var cylinderStart = new FixedPointVector3(0, -2, 0);
            var cylinderEnd = new FixedPointVector3(0, 2, 0);
            var cylinderRadius = (FixedPoint64)0.5;

            var aabbCollision = FixedPointIntersection.IntersectWithAABBAndCylinder(
                boxMin, boxMax, cylinderStart, cylinderEnd, cylinderRadius);
            AddRegressionResult("AABB/Cylinder", aabbCollision.hit && !aabbCollision.normal.IsZero(),
                aabbCollision.hit ? $"depth={aabbCollision.depth}" : "未命中");

            var obbCollision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                FixedPointVector3.zero, FixedPointVector3.one,
                FixedPointMatrix.Identity, cylinderStart, cylinderEnd, cylinderRadius);
            AddRegressionResult("OBB/Cylinder", obbCollision.hit && !obbCollision.normal.IsZero(),
                obbCollision.hit ? $"depth={obbCollision.depth}" : "未命中");

            var overlapCollision = FixedPointIntersection.IntersectWithCylinderAndCylinder(
                cylinderStart, cylinderEnd, cylinderRadius,
                new FixedPointVector3(0.75, -2, 0), new FixedPointVector3(0.75, 2, 0), cylinderRadius);
            AddRegressionResult("Cylinder/Cylinder 重叠", overlapCollision.hit,
                overlapCollision.hit ? $"depth={overlapCollision.depth}" : "未命中");

            var separatedCollision = FixedPointIntersection.IntersectWithCylinderAndCylinder(
                cylinderStart, cylinderEnd, cylinderRadius,
                new FixedPointVector3(3, -2, 0), new FixedPointVector3(3, 2, 0), cylinderRadius);
            AddRegressionResult("Cylinder/Cylinder 分离", !separatedCollision.hit,
                separatedCollision.hit ? "错误命中" : null);
        }

        private void TestCylinderSphereCastRegression()
        {
            if (m_cylinderTarget == null)
            {
                AddRegressionResult("SphereCast/Cylinder", false, "缺少 Cylinder_Target");
                return;
            }

            var origin = m_cylinderTarget.position - FixedPointVector3.right *
                (m_cylinderTarget.scaledRadius + 2);
            var originalLayer = m_cylinderTarget.layer;
            bool hit;
            FPRaycastHit result;
            try
            {
                m_cylinderTarget.layer = 31;
                hit = m_context.SphereCast(origin, (FixedPoint64)0.25,
                    FixedPointVector3.right, 6, out result, 1 << 31, true);
            }
            finally
            {
                m_cylinderTarget.layer = originalLayer;
            }
            AddRegressionResult("SphereCast/Cylinder", hit && result?.fpCollider == m_cylinderTarget,
                hit && result != null ? $"实际={result.fpCollider.name}, distance={result.distance}" : "未命中");
        }

        private void TestMeshSphereCastRegression()
        {
            if (m_meshTarget == null)
            {
                AddRegressionResult("SphereCast/Mesh", false, "缺少 Mesh_Target");
                return;
            }

            var origin = m_meshTarget.position + FixedPointVector3.up * 4;
            var hit = m_context.SphereCast(
                origin, (FixedPoint64)0.25, FixedPointVector3.down, 8,
                out var result, -1, true);
            AddRegressionResult("SphereCast/Mesh",
                hit && result?.fpCollider == m_meshTarget &&
                result.distance >= FixedPoint64.Zero && !result.normal.IsZero(),
                hit && result != null ? $"实际={result.fpCollider.name}, distance={result.distance}" : "未命中");
        }

        private void TestOverlapRegression()
        {
            TestOverlaySphereTarget(m_sphereTarget, "Sphere");
            TestOverlaySphereTarget(m_aabbTarget, "AABB");
            TestOverlaySphereTarget(m_obbTarget, "OBB");
            TestOverlaySphereTarget(m_capsuleTarget, "Capsule");
            TestOverlaySphereTarget(m_cylinderTarget, "Cylinder");
            TestOverlaySphereTarget(m_aaCapsuleTarget, "AACapsule");
            TestOverlaySphereTarget(m_characterTarget, "Character");

            TestOverlapSphereNonAllocTarget(m_sphereTarget, "Sphere");
            TestOverlapSphereNonAllocTarget(m_aabbTarget, "AABB");
            TestOverlapSphereNonAllocTarget(m_obbTarget, "OBB");
            TestOverlapSphereNonAllocTarget(m_capsuleTarget, "Capsule");
            TestOverlapSphereNonAllocTarget(m_cylinderTarget, "Cylinder");
            TestOverlapSphereNonAllocTarget(m_aaCapsuleTarget, "AACapsule");
            TestOverlapSphereNonAllocTarget(m_characterTarget, "Character");

            if (m_sphereTarget != null)
            {
                TestOverlayBoxTarget(m_sphereTarget, "Sphere");
            }
            TestOverlayBoxTarget(m_aabbTarget, "AABB");
            TestOverlayBoxTarget(m_obbTarget, "OBB");
            TestOverlayBoxTarget(m_capsuleTarget, "Capsule");
            TestOverlayBoxTarget(m_aaCapsuleTarget, "AACapsule");
            TestOverlayBoxTarget(m_cylinderTarget, "Cylinder");
            TestOverlayBoxTarget(m_meshTarget, "Mesh");
            TestOverlayBoxTarget(m_characterTarget, "Character");
        }

        private void TestColliderOverlayRegression()
        {
            TestColliderOverlayPair(m_cylinderTarget, m_aabbTarget, "Cylinder/AABB");
            TestColliderOverlayPair(m_cylinderTarget, m_obbTarget, "Cylinder/OBB");
            TestColliderOverlayPair(m_cylinderTarget, m_meshTarget, "Cylinder/Mesh");
            TestColliderOverlayPair(m_capsuleTarget, m_meshTarget, "Capsule/Mesh");
            TestColliderOverlayPair(m_obbTarget, m_meshTarget, "OBB/Mesh");
            TestColliderOverlayPair(m_meshTarget, m_cylinderTarget, "Mesh/Cylinder");
        }

        private void TestColliderOverlayPair(FPCollider query, FPCollider target, string label)
        {
            if (query == null || target == null)
            {
                AddRegressionResult($"OverlayCollider/{label}", false, "场景目标缺失");
                return;
            }

            var queryTransform = query.transform;
            var originalPosition = queryTransform.position;
            var originalRotation = queryTransform.rotation;
            var originalScale = queryTransform.localScale;

            try
            {
                queryTransform.position = target.transform.position;
                queryTransform.rotation = target.transform.rotation;
                SyncColliderTransform(query);
                m_overlapCollisions.Clear();
                var count = m_context.OverlayColliderCollision(
                    query, ref m_overlapCollisions, -1, true);
                AddRegressionResult(
                    $"OverlayCollider/{label}",
                    ContainsCollider(m_overlapCollisions, count, target),
                    $"结果数={count}");
            }
            finally
            {
                queryTransform.position = originalPosition;
                queryTransform.rotation = originalRotation;
                queryTransform.localScale = originalScale;
                SyncColliderTransform(query);
            }
        }

        private static void SyncColliderTransform(FPCollider collider)
        {
            collider.fpTransform.position = new FixedPointVector3(collider.transform.position);
            collider.fpTransform.rotation = new FixedPointQuaternion(collider.transform.rotation);
            collider.fpTransform.localScale = new FixedPointVector3(collider.transform.lossyScale);
            collider.UpdateCollider();
        }

        private void TestOverlayBoxTarget(FPCollider target, string label)
        {
            if (target == null)
            {
                AddRegressionResult($"OverlayBox/{label}", false, "场景目标缺失");
                return;
            }

            var center = (target.min + target.max) * FixedPoint64.Half;
            var halfSize = (target.max - target.min) * FixedPoint64.Half +
                           FixedPointVector3.one * (FixedPoint64)0.1;
            var collisions = m_context.OverlayBoxCollision(
                center, halfSize, FixedPointMatrix.Identity, -1, true);
            AddRegressionResult($"OverlayBox/{label}", ContainsCollider(collisions, target),
                $"结果数={collisions.Count}");
        }

        private void TestOverlaySphereTarget(FPCollider target, string label)
        {
            if (target == null)
            {
                AddRegressionResult($"OverlaySphere/{label}", false, "场景目标缺失");
                return;
            }

            m_overlapCollisions.Clear();
            var count = m_context.OverlaySphereCollision(target.position, (FixedPoint64)0.1,
                ref m_overlapCollisions, -1, true);
            AddRegressionResult($"OverlaySphere/{label}", ContainsCollider(m_overlapCollisions, count, target),
                $"结果数={count}");
        }

        private void TestOverlapSphereNonAllocTarget(FPCollider target, string label)
        {
            if (target == null)
            {
                AddRegressionResult($"NonAlloc/{label}", false, "场景目标缺失");
                return;
            }

            System.Array.Clear(m_nonAllocColliders, 0, m_nonAllocColliders.Length);
            var count = m_context.fpOctree.OverlapSphereNonAlloc(
                m_nonAllocColliders, target.position, (FixedPoint64)0.1, -1);
            AddRegressionResult($"NonAlloc/{label}", ContainsCollider(m_nonAllocColliders, count, target),
                $"结果数={count}");
        }

        private void TestNegativeScaleRegression()
        {
            if (m_sphereTarget == null)
            {
                AddRegressionResult("负缩放/Sphere", false, "缺少 Sphere_Target");
                return;
            }

            var originalScale = m_sphereTarget.fpTransform.localScale;

            try
            {
                m_sphereTarget.fpTransform.localScale = new FixedPointVector3(-2, -1, -3);
                m_sphereTarget.UpdateCollider();
                var expectedRadius = m_sphereTarget.radius * 3;
                var queryCenter = m_sphereTarget.position + FixedPointVector3.right *
                    (expectedRadius - (FixedPoint64)0.05);
                m_overlapCollisions.Clear();
                var count = m_context.OverlaySphereCollision(queryCenter, (FixedPoint64)0.1,
                    ref m_overlapCollisions, -1, true);
                AddRegressionResult("负缩放使用绝对世界半径",
                    m_sphereTarget.scaledRadius == expectedRadius &&
                    ContainsCollider(m_overlapCollisions, count, m_sphereTarget),
                    $"scaledRadius={m_sphereTarget.scaledRadius}, 结果数={count}");
            }
            finally
            {
                m_sphereTarget.fpTransform.localScale = originalScale;
                m_sphereTarget.UpdateCollider();
            }
        }

        private void AddRegressionResult(string name, bool passed, string detail)
        {
            m_regressionResults.Add(new RegressionResult
            {
                Name = name,
                Passed = passed,
                Detail = detail
            });
        }

        private static bool ContainsCollider(List<FPCollision> collisions, FPCollider target)
        {
            return ContainsCollider(collisions, collisions.Count, target);
        }

        private static bool ContainsCollider(List<FPCollision> collisions, int count, FPCollider target)
        {
            for (var i = 0; i < count; i++)
            {
                if (collisions[i].collider == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCollider(FPCollider[] colliders, int count, FPCollider target)
        {
            for (var i = 0; i < count; i++)
            {
                if (colliders[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCollider(List<FPRaycastHit> hits, FPCollider target)
        {
            foreach (var hit in hits)
            {
                if (hit.fpCollider == target) return true;
            }

            return false;
        }

        private static bool ContainsCollider(FPRaycastHit[] hits, int count, FPCollider target)
        {
            for (var i = 0; i < count; i++)
            {
                if (hits[i]?.fpCollider == target) return true;
            }

            return false;
        }

        private static bool IsSorted(List<FPRaycastHit> hits)
        {
            for (var i = 1; i < hits.Count; i++)
            {
                if (hits[i - 1].distance > hits[i].distance) return false;
            }

            return true;
        }

        private static bool IsSorted(FPRaycastHit[] hits, int count)
        {
            for (var i = 1; i < count; i++)
            {
                if (hits[i - 1].distance > hits[i].distance) return false;
            }

            return true;
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

            m_regressionSummary.Clear();
            var passedCount = 0;

            foreach (var result in m_regressionResults)
            {
                if (result.Passed)
                {
                    passedCount++;
                    continue;
                }

                if (m_regressionSummary.Length > 0)
                {
                    m_regressionSummary.Append("\n");
                }

                m_regressionSummary.Append("✗ ").Append(result.Name);
                if (!string.IsNullOrEmpty(result.Detail))
                {
                    m_regressionSummary.Append("：").Append(result.Detail);
                }
            }

            GUI.Box(new Rect(16f, 466f, 720f, 176f), GUIContent.none);
            GUI.Label(new Rect(32f, 478f, 500f, 22f),
                $"场景回归基线：{passedCount}/{m_regressionResults.Count} 通过");
            var failedCount = m_regressionResults.Count - passedCount;
            var contentHeight = Mathf.Max(88f, failedCount * 20f + 8f);
            m_regressionScroll = GUI.BeginScrollView(
                new Rect(32f, 504f, 686f, 92f),
                m_regressionScroll,
                new Rect(0f, 0f, 660f, contentHeight));
            GUI.Label(new Rect(0f, 0f, 650f, contentHeight),
                m_regressionSummary.Length == 0 ? "全部通过" : m_regressionSummary.ToString());
            GUI.EndScrollView();

            if (GUI.Button(new Rect(32f, 600f, 205f, 32f), "重新运行回归检查"))
            {
                RunRegressionTests();
            }

            GUI.Box(new Rect(16f, 654f, 720f, 72f), GUIContent.none);
            GUI.Label(new Rect(32f, 664f, 680f, 22f),
                $"可视化运动：{m_visualCaseName}  {(m_visualHit ? "命中" : "接近中")}");
            var oldColor = GUI.color;
            GUI.color = m_visualHit
                ? new Color(0.15f, 0.95f, 0.35f)
                : new Color(0.1f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(32f, 696f, 670f * m_visualProgress, 12f), Texture2D.whiteTexture);
            GUI.color = oldColor;
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

        private struct RegressionResult
        {
            public string Name;
            public bool Passed;
            public string Detail;
        }
    }
}
#endif
