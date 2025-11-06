using UnityEngine;

namespace Launcher
{
    public class UIBase : MonoBehaviour
    {
        protected object Param;

        public virtual void OnEnter(object param)
        {
            Param = param;
        }
    }
}