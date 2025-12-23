using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    [FilePath("ProjectSettings/FrameImportConfig.asset")]
    public class FrameImportConfig : EditorScriptableSingleton<FrameImportConfig>
    {
        [Header("目录设置")] [Tooltip("序列帧导入根目录")]
        [SerializeField] public string importFrameRootDir = "Assets/BundleAssets/FrameSprite";
        [SerializeField] public string frameConfigGenerateDir = "Assets/BundleAssets/Configs/FrameConfig";
        public string ImportFrameRootDir { get  => importFrameRootDir; set => importFrameRootDir = value; }

        [SerializeField] private List<FrameAnimName> frameAnimNames = new List<FrameAnimName>()
        {
            FrameAnimName.idle5,
            FrameAnimName.idle4,
            FrameAnimName.idle3,
            FrameAnimName.idle2,
            FrameAnimName.idle1,
            FrameAnimName.idle,
            FrameAnimName.walk1,
            FrameAnimName.walk2,
            FrameAnimName.walk3,
            FrameAnimName.walk4,
            FrameAnimName.walk5,
            FrameAnimName.walk,
            FrameAnimName.appear5,
            FrameAnimName.appear4,
            FrameAnimName.appear3,
            FrameAnimName.appear2,
            FrameAnimName.appear1,
            FrameAnimName.appear,
            FrameAnimName.death5,
            FrameAnimName.death4,
            FrameAnimName.death3,
            FrameAnimName.death2,
            FrameAnimName.death1,
            FrameAnimName.death,
            FrameAnimName.hurt5,
            FrameAnimName.hurt4,
            FrameAnimName.hurt3,
            FrameAnimName.hurt2,
            FrameAnimName.hurt1,
            FrameAnimName.hurt,
            FrameAnimName.run5,
            FrameAnimName.run4,
            FrameAnimName.run3,
            FrameAnimName.run2,
            FrameAnimName.run1,
            FrameAnimName.run,
            FrameAnimName.skill5,
            FrameAnimName.skill4,
            FrameAnimName.skill3,
            FrameAnimName.skill2,
            FrameAnimName.skill1,
            FrameAnimName.skill,
            FrameAnimName.attack5,
            FrameAnimName.attack4,
            FrameAnimName.attack3,
            FrameAnimName.attack2,
            FrameAnimName.attack1,
            FrameAnimName.attack,
            FrameAnimName.skill_prepare_loop,
            FrameAnimName.behit,
            FrameAnimName.loop,
            FrameAnimName.over,
        };
        public List<FrameAnimName> FrameAnimNames => frameAnimNames;

        [SerializeField] public FrameSpriteExtensionName frameSpriteExtensionName = FrameSpriteExtensionName.PNG;
        public FrameSpriteExtensionName FrameSpriteExtensionName => frameSpriteExtensionName;
        [SerializeField] public List<FrameAnimName> restPivotOrders = new List<FrameAnimName>
        {
            FrameAnimName.idle,
            FrameAnimName.death,
            FrameAnimName.run,
            FrameAnimName.walk,
            FrameAnimName.attack,
            FrameAnimName.skill,
        };
        public List<FrameAnimName> RestPivotOrders => restPivotOrders;

        public static string GetFrameSpriteExtensionName()
        {
            return Instance.FrameSpriteExtensionName switch
            {
                FrameSpriteExtensionName.PNG => "*.png",
                FrameSpriteExtensionName.JPG => "*.jpg",
                FrameSpriteExtensionName.JEPG => "*.jpeg",
                _ => "*.png",
            };
        }
    }
}