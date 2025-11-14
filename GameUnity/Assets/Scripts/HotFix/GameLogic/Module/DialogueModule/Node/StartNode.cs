using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace GameLogic
{
    // [NodeTint(0.2f, 0.8f, 0.4f)]
    public class StartNode : BaseNode
    {
        [Output, SerializeField] private BaseNode NextNode;

        public override object GetValue(NodePort port)
        {
            return 0.1;
        }
    }
}