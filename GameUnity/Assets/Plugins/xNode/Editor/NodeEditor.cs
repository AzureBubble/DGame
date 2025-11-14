using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif

namespace XNodeEditor {
    [CustomNodeEditor(typeof(XNode.Node))]
    public class NodeEditor : XNodeEditor.Internal.NodeEditorBase<NodeEditor, NodeEditor.CustomNodeEditorAttribute, XNode.Node> {

        private readonly Color DEFAULTCOLOR = new Color32(90, 97, 105, 255);
        public static Action<XNode.Node> onUpdateNode;
        public readonly static Dictionary<XNode.NodePort, Vector2> portPositions = new Dictionary<XNode.NodePort, Vector2>();

#if ODIN_INSPECTOR
        protected internal static bool inNodeEditor = false;
#endif

        public virtual void OnHeaderGUI() {
            GUILayout.Label(target.name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(30));
        }

        public virtual void OnBodyGUI() {
#if ODIN_INSPECTOR
            inNodeEditor = true;
#endif

            serializedObject.Update();
            string[] excludes = { "m_Script", "graph", "position", "ports" };

#if ODIN_INSPECTOR
            // 使用新的PropertyTree获取方式，确保使用using正确释放
            using (var propertyTree = GetPropertyTree()) {
                if (propertyTree != null) {
                    try {
                        propertyTree.BeginDraw(true);
                        GUIHelper.PushLabelWidth(84);
                        propertyTree.Draw(true);
                        propertyTree.EndDraw();
                        GUIHelper.PopLabelWidth();
                    }
                    catch (System.Exception e) {
                        Debug.LogError($"Error drawing Odin property tree: {e.Message}");
                        DrawDefaultProperties(excludes);
                    }
                } else {
                    DrawDefaultProperties(excludes);
                }
            } // 这里会自动调用Dispose()
#else
            DrawDefaultProperties(excludes);
#endif

            // 绘制动态端口
            foreach (XNode.NodePort dynamicPort in target.DynamicPorts) {
                if (NodeEditorGUILayout.IsDynamicPortListPort(dynamicPort)) continue;
                NodeEditorGUILayout.PortField(dynamicPort);
            }

            serializedObject.ApplyModifiedProperties();

#if ODIN_INSPECTOR
            if (GUIHelper.RepaintRequested) {
                GUIHelper.ClearRepaintRequest();
                window.Repaint();
            }
            inNodeEditor = false;
#endif
        }

        private void DrawDefaultProperties(string[] excludes) {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren)) {
                enterChildren = false;
                if (excludes.Contains(iterator.name)) continue;
                NodeEditorGUILayout.PropertyField(iterator, true);
            }
        }

        public override void OnDestroy() {
            base.OnDestroy();
        }

        // 其他方法保持不变...
        public virtual int GetWidth() {
            Type type = target.GetType();
            int width;
            if (type.TryGetAttributeWidth(out width)) return width;
            else return 208;
        }

        public virtual Color GetTint() {
            Type type = target.GetType();
            Color color;
            if (type.TryGetAttributeTint(out color)) return color;
            else return DEFAULTCOLOR;
        }

        public virtual GUIStyle GetBodyStyle() {
            return NodeEditorResources.styles.nodeBody;
        }

        public virtual GUIStyle GetBodyHighlightStyle() {
            return NodeEditorResources.styles.nodeHighlight;
        }

        public virtual void AddContextMenuItems(GenericMenu menu) {
            bool canRemove = true;
            if (Selection.objects.Length == 1 && Selection.activeObject is XNode.Node) {
                XNode.Node node = Selection.activeObject as XNode.Node;
                menu.AddItem(new GUIContent("Move To Top"), false, () => NodeEditorWindow.current.MoveNodeToTop(node));
                menu.AddItem(new GUIContent("Rename"), false, NodeEditorWindow.current.RenameSelectedNode);
                canRemove = NodeGraphEditor.GetEditor(node.graph, NodeEditorWindow.current).CanRemove(node);
            }

            menu.AddItem(new GUIContent("Copy"), false, NodeEditorWindow.current.CopySelectedNodes);
            menu.AddItem(new GUIContent("Duplicate"), false, NodeEditorWindow.current.DuplicateSelectedNodes);

            if (canRemove) menu.AddItem(new GUIContent("Remove"), false, NodeEditorWindow.current.RemoveSelectedNodes);
            else menu.AddItem(new GUIContent("Remove"), false, null);

            if (Selection.objects.Length == 1 && Selection.activeObject is XNode.Node) {
                XNode.Node node = Selection.activeObject as XNode.Node;
                menu.AddCustomContextMenuItems(node);
            }
        }

        public void Rename(string newName) {
            if (newName == null || newName.Trim() == "") newName = NodeEditorUtilities.NodeDefaultName(target.GetType());
            target.name = newName;
            OnRename();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }

        public virtual void OnRename() { }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeEditorAttribute : Attribute,
        XNodeEditor.Internal.NodeEditorBase<NodeEditor, NodeEditor.CustomNodeEditorAttribute, XNode.Node>.INodeEditorAttrib {
            private Type inspectedType;
            public CustomNodeEditorAttribute(Type inspectedType) {
                this.inspectedType = inspectedType;
            }

            public Type GetInspectedType() {
                return inspectedType;
            }
        }
    }
}