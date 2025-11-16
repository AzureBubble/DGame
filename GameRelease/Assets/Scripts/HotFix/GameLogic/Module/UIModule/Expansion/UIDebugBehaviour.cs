using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GameLogic
{
    [DisallowMultipleComponent]
    public class UIDebugBehaviour : MonoBehaviour
    {
        [Conditional("ENABLE_DGAME_LOG")]
        public static void AddUIDebugBehaviour(GameObject go)
        {
            if (!go.TryGetComponent<UIDebugBehaviour>(out var uiDebugBehaviour))
            {
                go.AddComponent<UIDebugBehaviour>();
            }
        }
    }
}