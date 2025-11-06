using System;
using UnityEngine;

namespace DGame
{
    [Serializable]
    public class AnimationWrapper
    {
        public int Layer;
        public WrapMode WrapMode;
        public AnimationClip Clip;
    }
}