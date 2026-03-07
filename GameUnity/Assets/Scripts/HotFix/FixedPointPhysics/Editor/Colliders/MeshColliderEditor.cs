using UnityEditor;

namespace GameBattle.Editor
{
    /// <summary>
    /// A custom editor for the MeshCollider component.
    /// </summary>
    [CustomEditor(typeof(FPMeshCollider))]
    [CanEditMultipleObjects]
    public class MeshColliderEditor : ColliderEditor
    {
    }
}