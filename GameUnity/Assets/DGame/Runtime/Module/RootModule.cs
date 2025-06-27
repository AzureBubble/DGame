using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    [DisallowMultipleComponent]
    public sealed class RootModule : MonoBehaviour
    {
        private static RootModule m_instance = null;

        public static RootModule Instance => m_instance == null ? UnityEngine.Object.FindObjectOfType<RootModule>() : m_instance;

        private void Awake()
        {
            m_instance = this;

            GameTime.StartFrame();
        }

        private void Update()
        {
            GameTime.StartFrame();
            ModuleSystem.Update(GameTime.DeltaTime, GameTime.UnscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            GameTime.StartFrame();
        }

        private void LateUpdate()
        {
            GameTime.StartFrame();
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
            ModuleSystem.OnDestroy();
#endif
        }

        internal void Destroy()
        {
            Destroy(gameObject);
        }
    }
}