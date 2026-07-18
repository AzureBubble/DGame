#if UNITY_2021_3_OR_NEWER
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DGame.FixedPoint.Samples
{
    /// <summary>
    /// 定点物理碰撞测试场景驱动器。
    /// 将 Scene 视图中的 Unity Transform 同步到 FPTransform，并用球形探针检测全部碰撞器类型。
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class FixedPointPhysicsTestController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private FPSphereCollider m_probe;
        [SerializeField] private Color m_idleColor = new(0.15f, 0.45f, 0.95f, 1f);
        [SerializeField] private Color m_candidateColor = new(1f, 0.78f, 0.08f, 1f);
        [SerializeField] private Color m_hitColor = new(0.15f, 0.9f, 0.3f, 1f);
        [SerializeField] private Color m_probeColor = new(1f, 0.55f, 0.08f, 1f);
        [SerializeField] private bool m_drawOctreeNodes = true;

        private List<FPCollision> m_activeCollisions = new();
        private readonly List<FPCollider> m_colliders = new();
        private readonly Dictionary<FPCollider, Renderer[]> m_renderers = new();
        private readonly Dictionary<FPCollider, TransformSnapshot> m_transformSnapshots = new();
        private readonly HashSet<FPCollider> m_candidateColliders = new();
        private readonly HashSet<FPCollider> m_hitColliders = new();
        private readonly List<FPOctreeNode> m_openNodes = new();
        private readonly List<Transform> m_auxiliaryDragTargets = new();
        private readonly List<int> m_meshTriangleCandidates = new();
        private MaterialPropertyBlock m_propertyBlock;
        private readonly StringBuilder m_hitNames = new();
        private readonly StringBuilder m_candidateNames = new();
        private readonly StringBuilder m_unsupportedNames = new();

        private FPPhysicsContext m_context;
        private FPCharacterController m_character;
        private FPCollider m_activeProbe;
        private FPCollider m_draggedCollider;
        private Transform m_draggedTransform;
        private Camera m_mainCamera;
        private Plane m_dragPlane;
        private Vector3 m_dragOffset;
        private GUIStyle m_titleStyle;
        private GUIStyle m_bodyStyle;

        private void Awake()
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            m_context = FPPhysicsPresenter.Instance.context;
            m_mainCamera = Camera.main;
            CacheSceneObjects();

            // 测试场景只验证拖拽碰撞，不让角色控制器的重力模拟覆盖手动拖拽位置。
            if (m_character != null)
            {
                m_context.RemoveCharacter(m_character);
            }

            if (m_probe != null)
            {
                SetActiveProbe(m_probe);
            }
            else if (m_colliders.Count > 0)
            {
                SetActiveProbe(m_colliders[0]);
            }
            SyncAllTransforms(true);
            DetectCollisions();
        }

        private void Update()
        {
            if (m_context == null || m_activeProbe == null)
            {
                return;
            }

            UpdateManualDrag();
            SyncAllTransforms(false);
            DetectCollisions();
        }

        private void UpdateManualDrag()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main;
            }

            if (m_mainCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverTestUi())
            {
                BeginManualDrag(m_mainCamera.ScreenPointToRay(Input.mousePosition));
            }

            if (m_draggedTransform != null && Input.GetMouseButton(0))
            {
                var ray = m_mainCamera.ScreenPointToRay(Input.mousePosition);
                if (m_dragPlane.Raycast(ray, out var enter))
                {
                    m_draggedTransform.position = ray.GetPoint(enter) + m_dragOffset;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                m_draggedCollider = null;
                m_draggedTransform = null;
            }
        }

        private void BeginManualDrag(Ray ray)
        {
            FPCollider nearestCollider = null;
            Transform nearestTransform = null;
            var nearestDistance = float.MaxValue;

            foreach (var collider in m_colliders)
            {
                if (collider == null || !collider.enabled ||
                    !m_renderers.TryGetValue(collider, out var renderers))
                {
                    continue;
                }

                foreach (var targetRenderer in renderers)
                {
                    if (targetRenderer == null || !targetRenderer.enabled ||
                        !targetRenderer.gameObject.activeInHierarchy ||
                        !targetRenderer.bounds.IntersectRay(ray, out var distance) ||
                        distance >= nearestDistance)
                    {
                        continue;
                    }

                    nearestDistance = distance;
                    nearestCollider = collider;
                    nearestTransform = collider.transform;
                }
            }

            foreach (var target in m_auxiliaryDragTargets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                foreach (var targetRenderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    if (targetRenderer == null || !targetRenderer.enabled ||
                        !targetRenderer.gameObject.activeInHierarchy ||
                        !targetRenderer.bounds.IntersectRay(ray, out var distance) ||
                        distance >= nearestDistance)
                    {
                        continue;
                    }

                    nearestDistance = distance;
                    nearestCollider = null;
                    nearestTransform = target;
                }
            }

            if (nearestTransform == null)
            {
                return;
            }

            var dragPosition = nearestTransform.position;
            m_dragPlane = new Plane(Vector3.up, dragPosition);
            if (!m_dragPlane.Raycast(ray, out var enter))
            {
                return;
            }

            m_dragOffset = dragPosition - ray.GetPoint(enter);
            m_draggedCollider = nearestCollider;
            m_draggedTransform = nearestTransform;

            if (nearestCollider != null)
            {
                SetActiveProbe(nearestCollider);
            }
        }

        private static bool IsPointerOverTestUi()
        {
            var mousePosition = Input.mousePosition;
            var guiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            return new Rect(16f, 16f, 720f, 710f).Contains(guiPosition);
        }

        private void LateUpdate()
        {
            var cameraTransform = Camera.main != null ? Camera.main.transform : null;

            if (cameraTransform == null)
            {
                return;
            }

            foreach (var textMesh in GetComponentsInChildren<TextMesh>(true))
            {
                textMesh.transform.rotation = cameraTransform.rotation;
            }
        }

        private void CacheSceneObjects()
        {
            m_colliders.Clear();
            m_renderers.Clear();
            m_transformSnapshots.Clear();
            m_auxiliaryDragTargets.Clear();
            m_character = null;

            foreach (var collider in GetComponentsInChildren<FPCollider>(true))
            {
                m_colliders.Add(collider);
                m_renderers[collider] = collider.GetComponentsInChildren<Renderer>(true);

                if (collider is FPCharacterController character)
                {
                    m_character = character;
                }
            }

            if (m_probe == null)
            {
                m_probe = m_colliders.Find(collider => collider.name == "Sphere_Probe") as FPSphereCollider;
            }

            foreach (var collider in m_colliders)
            {
                collider.isDynamic = false;
            }

            RegisterAuxiliaryDragTarget("RayPlane_Target");
            RegisterAuxiliaryDragTarget("RayTriangle_Target");
            RegisterAuxiliaryDragTarget("HighSpeedCollisionEmitter");
            RegisterAuxiliaryDragTarget("HighSpeedRayEmitter");
        }

        private void RegisterAuxiliaryDragTarget(string objectName)
        {
            var target = transform.Find(objectName);
            if (target == null)
            {
                var gameObject = GameObject.Find(objectName);
                target = gameObject != null ? gameObject.transform : null;
            }

            if (target == null || target.GetComponentInParent<FPCollider>() != null ||
                target.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                return;
            }

            m_auxiliaryDragTargets.Add(target);
        }

        private void SyncAllTransforms(bool force)
        {
            foreach (var collider in m_colliders)
            {
                if (collider == null || collider.fpTransform == null)
                {
                    continue;
                }

                var unityTransform = collider.transform;
                var snapshot = new TransformSnapshot(
                    unityTransform.position,
                    unityTransform.rotation,
                    unityTransform.lossyScale);

                if (!force && m_transformSnapshots.TryGetValue(collider, out var previousSnapshot) &&
                    snapshot.Equals(previousSnapshot))
                {
                    continue;
                }

                collider.fpTransform.position = new FixedPointVector3(unityTransform.position);
                collider.fpTransform.rotation = new FixedPointQuaternion(unityTransform.rotation);
                collider.fpTransform.localScale = new FixedPointVector3(unityTransform.lossyScale);
                collider.UpdateCollider();
                m_transformSnapshots[collider] = snapshot;
                unityTransform.hasChanged = false;

                if (!force)
                {
                    SetActiveProbe(collider);
                }
            }
        }

        private void SetActiveProbe(FPCollider collider)
        {
            if (collider == null || collider == m_activeProbe)
            {
                return;
            }

            if (m_activeProbe != null)
            {
                m_activeProbe.isDynamic = false;
            }

            m_activeProbe = collider;
            m_activeProbe.isDynamic = true;
        }

        private void DetectCollisions()
        {
            m_hitColliders.Clear();
            m_activeCollisions.Clear();
            m_unsupportedNames.Clear();
            CollectBroadPhaseCandidates();

            m_context.OverlayColliderCollision(m_activeProbe, ref m_activeCollisions, -1, true);

            foreach (var collision in m_activeCollisions)
            {
                var collider = collision.collider;
                if (collider == null || collider == m_activeProbe)
                {
                    continue;
                }

                m_hitColliders.Add(collider);
            }

            foreach (var collider in m_colliders)
            {
                var color = collider == m_activeProbe
                    ? m_probeColor
                    : m_hitColliders.Contains(collider)
                        ? m_hitColor
                        : m_candidateColliders.Contains(collider)
                            ? m_candidateColor
                        : m_idleColor;
                SetColliderColor(collider, color);
            }
        }

        private void CollectBroadPhaseCandidates()
        {
            m_candidateColliders.Clear();
            m_openNodes.Clear();

            var root = m_context?.fpOctree?.root;

            if (root == null || m_activeProbe == null)
            {
                return;
            }

            m_openNodes.Add(root);

            for (var nodeIndex = 0; nodeIndex < m_openNodes.Count; nodeIndex++)
            {
                var node = m_openNodes[nodeIndex];
                AddNodeCandidates(node);

                if (node.nodes == null)
                {
                    continue;
                }

                foreach (var child in node.nodes)
                {
                    if (child.colliderCount <= 0)
                    {
                        continue;
                    }

                    if (FixedPointIntersection.IntersectWithAABBAndAABBFixedPoint(
                            m_activeProbe.min,
                            m_activeProbe.max,
                            child.fixedPointAABB.Min,
                            child.fixedPointAABB.Max))
                    {
                        m_openNodes.Add(child);
                    }
                }
            }

            m_candidateColliders.Remove(m_activeProbe);
        }

        private void AddNodeCandidates(FPOctreeNode node)
        {
            AddCandidates(node.FpSphereColliders);
            AddCandidates(node.FpAABBColliders);
            AddCandidates(node.FpObbColliders);
            AddCandidates(node.FpCapsuleColliders);
            AddCandidates(node.FpCylinderColliders);
            AddCandidates(node.FpAACapsuleColliders);
            AddCandidates(node.FpMeshColliders);
            AddCandidates(node.FpCharacterColliders);
        }

        private void AddCandidates<T>(FPOctreeColliderSet<T> colliders) where T : FPCollider
        {
            if (colliders == null)
            {
                return;
            }

            for (var i = 0; i < colliders.Count; i++)
            {
                var collider = colliders[i];

                if (collider != null && collider.enabled)
                {
                    m_candidateColliders.Add(collider);
                }
            }
        }

        private bool TryIntersect(FPCollider active, FPCollider target, out FPCollision collision)
        {
            if (TryGetSphere(active, out var sphereCenter, out var sphereRadius))
            {
                return TryIntersectSphereWithTarget(sphereCenter, sphereRadius, target, out collision);
            }

            if (TryGetSphere(target, out sphereCenter, out sphereRadius))
            {
                var supported = TryIntersectSphereWithTarget(sphereCenter, sphereRadius, active, out collision);
                FlipCollision(ref collision);
                return supported;
            }

            var activeIsCapsule = TryGetCapsule(active, out var activeStart, out var activeEnd,
                out var activeRadius);
            var targetIsCapsule = TryGetCapsule(target, out var targetStart, out var targetEnd,
                out var targetRadius);

            if (activeIsCapsule && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCapsule(
                    activeStart, activeEnd, activeRadius, targetStart, targetEnd, targetRadius);
                return true;
            }

            if (activeIsCapsule && target is FPCylinderCollider targetCylinder)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCylinder(
                    activeStart, activeEnd, activeRadius,
                    targetCylinder.startPos, targetCylinder.endPos, targetCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider activeCylinder && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndCylinder(
                    targetStart, targetEnd, targetRadius,
                    activeCylinder.startPos, activeCylinder.endPos, activeCylinder.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPCylinderCollider firstCylinder &&
                target is FPCylinderCollider secondCylinder)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndCylinder(
                    firstCylinder.startPos, firstCylinder.endPos, firstCylinder.scaledRadius,
                    secondCylinder.startPos, secondCylinder.endPos, secondCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderProbe && target is FPMeshCollider cylinderMesh)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndMesh(
                    cylinderProbe.startPos, cylinderProbe.endPos, cylinderProbe.scaledRadius,
                    cylinderMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider meshCylinder && target is FPCylinderCollider cylinderTarget)
            {
                collision = FixedPointIntersection.IntersectWithCylinderAndMesh(
                    cylinderTarget.startPos, cylinderTarget.endPos, cylinderTarget.scaledRadius,
                    meshCylinder, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPMeshCollider targetMesh)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                    activeStart, activeEnd, activeRadius, targetMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeMesh && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithCapsuleAndMesh(
                    targetStart, targetEnd, targetRadius, activeMesh, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider activeObbMesh && target is FPMeshCollider targetObbMesh)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    activeObbMesh.position, activeObbMesh.halfSize,
                    activeObbMesh.fpTransform.fixedPointMatrix,
                    activeObbMesh.min, activeObbMesh.max,
                    targetObbMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeObbMeshTarget && target is FPBoxCollider targetObbMeshBox)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    targetObbMeshBox.position, targetObbMeshBox.halfSize,
                    targetObbMeshBox.fpTransform.fixedPointMatrix,
                    targetObbMeshBox.min, targetObbMeshBox.max,
                    activeObbMeshTarget, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider activeAabbMesh && active is not FPBoxCollider &&
                target is FPMeshCollider targetAabbMesh)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    activeAabbMesh.position, activeAabbMesh.halfSize, FixedPointMatrix.Identity,
                    activeAabbMesh.min, activeAabbMesh.max,
                    targetAabbMesh, m_meshTriangleCandidates);
                return true;
            }

            if (active is FPMeshCollider activeAabbMeshTarget &&
                target is FPAABBCollider targetAabbMeshBox && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndMesh(
                    targetAabbMeshBox.position, targetAabbMeshBox.halfSize, FixedPointMatrix.Identity,
                    targetAabbMeshBox.min, targetAabbMeshBox.max,
                    activeAabbMeshTarget, m_meshTriangleCandidates);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPBoxCollider targetObb)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                    activeStart, activeEnd, activeRadius,
                    targetObb.position, targetObb.halfSize, targetObb.fpTransform.fixedPointMatrix,
                    targetObb.min, targetObb.max);
                return true;
            }

            if (active is FPBoxCollider activeObb && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndOBB(
                    targetStart, targetEnd, targetRadius,
                    activeObb.position, activeObb.halfSize, activeObb.fpTransform.fixedPointMatrix,
                    activeObb.min, activeObb.max);
                FlipCollision(ref collision);
                return true;
            }

            if (activeIsCapsule && target is FPAABBCollider targetAabb && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndAABB(
                    activeStart, activeEnd, activeRadius, targetAabb.min, targetAabb.max);
                return true;
            }

            if (active is FPAABBCollider activeAabb && active is not FPBoxCollider && targetIsCapsule)
            {
                collision = FixedPointIntersection.IntersectWithAACapsuleAndAABB(
                    targetStart, targetEnd, targetRadius, activeAabb.min, activeAabb.max);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider activeBox && active is not FPBoxCollider &&
                target is FPAABBCollider targetBox && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndAABBCollision(
                    activeBox.min, activeBox.max, targetBox.min, targetBox.max);
                return true;
            }

            if (active is FPAABBCollider cylinderAabb && active is not FPBoxCollider &&
                target is FPCylinderCollider aabbCylinder)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndCylinder(
                    cylinderAabb.min, cylinderAabb.max,
                    aabbCylinder.startPos, aabbCylinder.endPos, aabbCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderAabbTarget &&
                target is FPAABBCollider aabbCylinderTarget && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndCylinder(
                    aabbCylinderTarget.min, aabbCylinderTarget.max,
                    cylinderAabbTarget.startPos, cylinderAabbTarget.endPos,
                    cylinderAabbTarget.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider cylinderObb && target is FPCylinderCollider obbCylinder)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                    cylinderObb.position, cylinderObb.halfSize,
                    cylinderObb.fpTransform.fixedPointMatrix,
                    obbCylinder.startPos, obbCylinder.endPos, obbCylinder.scaledRadius);
                return true;
            }

            if (active is FPCylinderCollider cylinderObbTarget &&
                target is FPBoxCollider obbCylinderTarget)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndCylinder(
                    obbCylinderTarget.position, obbCylinderTarget.halfSize,
                    obbCylinderTarget.fpTransform.fixedPointMatrix,
                    cylinderObbTarget.startPos, cylinderObbTarget.endPos,
                    cylinderObbTarget.scaledRadius);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPAABBCollider axisAlignedBox && active is not FPBoxCollider &&
                target is FPBoxCollider orientedBox)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                    axisAlignedBox.min, axisAlignedBox.max,
                    orientedBox.position, orientedBox.halfSize,
                    orientedBox.fpTransform.fixedPointMatrix);
                return true;
            }

            if (active is FPBoxCollider orientedActive &&
                target is FPAABBCollider axisAlignedTarget && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithAABBAndOBBCollision(
                    axisAlignedTarget.min, axisAlignedTarget.max,
                    orientedActive.position, orientedActive.halfSize,
                    orientedActive.fpTransform.fixedPointMatrix);
                FlipCollision(ref collision);
                return true;
            }

            if (active is FPBoxCollider firstObb && target is FPBoxCollider secondObb)
            {
                collision = FixedPointIntersection.IntersectWithOBBAndOBBCollision(
                    firstObb.position, firstObb.halfSize, firstObb.fpTransform.fixedPointMatrix,
                    secondObb.position, secondObb.halfSize, secondObb.fpTransform.fixedPointMatrix);
                return true;
            }

            collision = default;
            return false;
        }

        private static bool TryIntersectSphereWithTarget(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FPCollider target,
            out FPCollision collision)
        {
            if (TryGetSphere(target, out var targetCenter, out var targetRadius))
            {
                collision = FixedPointIntersection.IntersectWithSphereAndSphere(
                    center, radius, targetCenter, targetRadius);
                return true;
            }

            if (target is FPBoxCollider obb)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndOBB(
                    center, radius, obb.position, obb.halfSize, obb.fpTransform.fixedPointMatrix);
                return true;
            }

            if (target is FPAABBCollider aabb && target is not FPBoxCollider)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndAABB(
                    center, radius, aabb.min, aabb.max);
                return true;
            }

            if (target is FPCylinderCollider cylinder)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndCylinder(
                    center, radius, cylinder.startPos, cylinder.endPos, cylinder.scaledRadius);
                return true;
            }

            if (TryGetCapsule(target, out var start, out var end, out var capsuleRadius))
            {
                collision = FixedPointIntersection.IntersectWithSphereAndCapsule(
                    center, radius, start, end, capsuleRadius);
                return true;
            }

            if (target is FPMeshCollider mesh)
            {
                collision = FixedPointIntersection.IntersectWithSphereAndMesh(center, radius, mesh);
                return true;
            }

            collision = default;
            return false;
        }

        private static bool TryGetSphere(
            FPCollider collider,
            out FixedPointVector3 center,
            out FixedPoint64 radius)
        {
            if (collider is FPSphereCollider sphere)
            {
                center = sphere.position;
                radius = sphere.scaledRadius;
                return true;
            }

            if (collider is FPCharacterController character &&
                character.characterColliderType == CharacterCollider.Sphere)
            {
                center = character.position;
                radius = character.scaledRadius;
                return true;
            }

            center = default;
            radius = default;
            return false;
        }

        private static bool TryGetCapsule(
            FPCollider collider,
            out FixedPointVector3 start,
            out FixedPointVector3 end,
            out FixedPoint64 radius)
        {
            if (collider is FPCharacterController character &&
                character.characterColliderType == CharacterCollider.Sphere)
            {
                start = default;
                end = default;
                radius = default;
                return false;
            }

            if (collider is FPAACapsuleCollider capsule)
            {
                start = capsule.startPos;
                end = capsule.endPos;
                radius = capsule.scaledRadius;
                return true;
            }

            start = default;
            end = default;
            radius = default;
            return false;
        }

        private static void FlipCollision(ref FPCollision collision)
        {
            collision.normal = -collision.normal;
            (collision.closestPoint, collision.outsidePoint) =
                (collision.outsidePoint, collision.closestPoint);
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 m_position;
            private readonly Quaternion m_rotation;
            private readonly Vector3 m_scale;

            public TransformSnapshot(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                m_position = position;
                m_rotation = rotation;
                m_scale = scale;
            }

            public bool Equals(TransformSnapshot other)
            {
                return (m_position - other.m_position).sqrMagnitude < 0.00000001f &&
                       1f - Mathf.Abs(Quaternion.Dot(m_rotation, other.m_rotation)) < 0.000001f &&
                       (m_scale - other.m_scale).sqrMagnitude < 0.00000001f;
            }
        }

        private void SetColliderColor(FPCollider collider, Color color)
        {
            if (collider == null || !m_renderers.TryGetValue(collider, out var renderers))
            {
                return;
            }

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetColor(BaseColorId, color);
                m_propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(m_propertyBlock);
                m_propertyBlock.Clear();
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_drawOctreeNodes)
            {
                FPPhysicsPresenter.Instance.DrawGizmos();
            }

            var previousColor = Gizmos.color;

            foreach (var collision in m_activeCollisions)
            {
                var contactPoint = collision.contactPoint.ToVector3();
                var normal = collision.normal.ToVector3();
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(contactPoint, 0.12f);
                Gizmos.DrawLine(contactPoint, contactPoint + normal * 1.25f);
            }

            Gizmos.color = previousColor;
        }

        private void OnGUI()
        {
            EnsureGuiStyles();
            m_hitNames.Clear();
            m_candidateNames.Clear();

            foreach (var collider in m_candidateColliders)
            {
                if (m_candidateNames.Length > 0)
                {
                    m_candidateNames.Append("、");
                }

                m_candidateNames.Append(collider.name);
            }

            foreach (var collider in m_hitColliders)
            {
                if (m_hitNames.Length > 0)
                {
                    m_hitNames.Append("、");
                }

                m_hitNames.Append(collider.name);
            }

            GUI.Box(new Rect(16f, 16f, 720f, 225f), GUIContent.none);
            GUI.Label(new Rect(32f, 27f, 590f, 28f), "DGame 定点物理碰撞测试", m_titleStyle);
            GUI.Label(
                new Rect(32f, 58f, 690f, 172f),
                "Game View 左键拖动物体；也可在 Scene 视图使用移动手柄。\n" +
                "蓝色=节点外　黄色=粗检测候选　绿色=精确命中　橙色=活动探针\n" +
                "紫色点/线=接触信息　红框=八叉树节点\n" +
                $"拖曳状态：{(m_draggedTransform == null ? "待选择" : $"正在拖动 {m_draggedTransform.name}")}\n" +
                $"当前探针：{(m_activeProbe == null ? "无" : m_activeProbe.name)}\n" +
                $"粗检测候选：{(m_candidateNames.Length == 0 ? "无" : m_candidateNames.ToString())}\n" +
                $"当前命中：{(m_hitNames.Length == 0 ? "无" : m_hitNames.ToString())}\n" +
                $"源码未实现精确组合：{(m_unsupportedNames.Length == 0 ? "无" : m_unsupportedNames.ToString())}",
                m_bodyStyle);
        }

        private void EnsureGuiStyles()
        {
            m_titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            m_bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }
    }
}
#endif
