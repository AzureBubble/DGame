using Sirenix.OdinInspector;
using UnityEngine;
using XNode;

namespace GameLogic
{
    [NodeTint(0.1f, 0.75f, 0.2f)]
    public class FinishNode : Node
    {
        [Input]
        [LabelText("完成节点")]
        [GUIColor(0f, 0.2f, 1f)] // 白色文字
        [SerializeField] private BaseNode PreNode;

        public override object GetValue(NodePort port)
        {
            return true;
        }
    }
}