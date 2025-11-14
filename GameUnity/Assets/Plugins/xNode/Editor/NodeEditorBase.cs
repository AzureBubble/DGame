using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace XNodeEditor.Internal {
    public abstract class NodeEditorBase<T, A, K> where A : Attribute, NodeEditorBase<T, A, K>.INodeEditorAttrib where T : NodeEditorBase<T, A, K> where K : ScriptableObject {
        private static Dictionary<Type, Type> editorTypes;
        private static Dictionary<K, T> editors = new Dictionary<K, T>();
        public NodeEditorWindow window;
        public K target;
        public SerializedObject serializedObject;

#if ODIN_INSPECTOR
        private PropertyTree _objectTree;
        private bool _objectTreeDisposed = false;
        public PropertyTree objectTree => _objectTree;

        // 修改PropertyTree获取方式，确保每次都是新的实例
        public PropertyTree GetPropertyTree() {
            // 如果已经释放，创建新的
            if (_objectTreeDisposed || _objectTree == null) {
                DisposePropertyTree(); // 确保清理旧的
                try {
                    bool wasInEditor = NodeEditor.inNodeEditor;
                    NodeEditor.inNodeEditor = true;
                    _objectTree = PropertyTree.Create(this.serializedObject);
                    NodeEditor.inNodeEditor = wasInEditor;
                    _objectTreeDisposed = false;
                } catch (ArgumentException ex) {
                    Debug.LogError("Failed to create PropertyTree: " + ex.Message);
                    return null;
                }
            }
            return _objectTree;
        }

        public void DisposePropertyTree() {
            if (_objectTree != null && !_objectTreeDisposed) {
                try {
                    _objectTree.Dispose();
                } catch (System.Exception e) {
                    Debug.LogWarning("Error disposing PropertyTree: " + e.Message);
                }
                _objectTree = null;
                _objectTreeDisposed = true;
            }
        }
#endif

        public static T GetEditor(K target, NodeEditorWindow window) {
            if (target == null) return null;
            T editor;
            if (!editors.TryGetValue(target, out editor)) {
                Type type = target.GetType();
                Type editorType = GetEditorType(type);
                editor = Activator.CreateInstance(editorType) as T;
                editor.target = target;
                editor.serializedObject = new SerializedObject(target);
                editor.window = window;
                editor.OnCreate();
                editors.Add(target, editor);
            }
            if (editor.target == null) editor.target = target;
            if (editor.window != window) editor.window = window;
            if (editor.serializedObject == null) editor.serializedObject = new SerializedObject(target);
            return editor;
        }

        // 其他方法保持不变...
        private static Type GetEditorType(Type type) {
            if (type == null) return null;
            if (editorTypes == null) CacheCustomEditors();
            Type result;
            if (editorTypes.TryGetValue(type, out result)) return result;
            return GetEditorType(type.BaseType);
        }

        private static void CacheCustomEditors() {
            editorTypes = new Dictionary<Type, Type>();
            Type[] nodeEditors = typeof(T).GetDerivedTypes();
            for (int i = 0; i < nodeEditors.Length; i++) {
                if (nodeEditors[i].IsAbstract) continue;
                var attribs = nodeEditors[i].GetCustomAttributes(typeof(A), false);
                if (attribs == null || attribs.Length == 0) continue;
                A attrib = attribs[0] as A;
                editorTypes.Add(attrib.GetInspectedType(), nodeEditors[i]);
            }
        }

        public virtual void OnCreate() { }

        public static void DisposeAllEditors() {
#if ODIN_INSPECTOR
            foreach (var editor in editors.Values) {
                editor.DisposePropertyTree();
            }
#endif
            editors.Clear();
        }

        public static void DisposeEditor(K target) {
            if (editors.TryGetValue(target, out T editor)) {
#if ODIN_INSPECTOR
                editor.DisposePropertyTree();
#endif
                editors.Remove(target);
            }
        }

        public virtual void OnDestroy() {
#if ODIN_INSPECTOR
            DisposePropertyTree();
#endif
        }

        public interface INodeEditorAttrib {
            Type GetInspectedType();
        }
    }
}