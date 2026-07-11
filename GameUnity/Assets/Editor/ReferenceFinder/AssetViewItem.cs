using UnityEditor.IMGUI.Controls;

#if UNITY_6000_2_OR_NEWER
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
#endif

namespace DGame
{
    internal sealed class AssetViewItem : TreeViewItem
    {
        public ReferenceFinderData.AssetDescription data;
    }
}
