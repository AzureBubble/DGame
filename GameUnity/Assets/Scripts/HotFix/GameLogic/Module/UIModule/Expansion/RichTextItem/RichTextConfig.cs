using System;
using System.Collections.Generic;
using System.Threading;
using DGame;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 富文本配置和资源加载集成
    /// 与项目的资源加载模块集成，用于加载图片资源
    /// </summary>
    public static class RichTextConfig
    {
        /// <summary>
        /// 图标前缀，用于解析 [icon:xxx] 格式的标签
        /// </summary>
        public static string IconPrefix { get; set; } = "icon:";

        private static readonly Dictionary<string, EmojiAnimationData> s_emojiRegistry =
            new Dictionary<string, EmojiAnimationData>();

        /// <summary>
        /// 注册带动画帧的表情
        /// </summary>
        /// <param name="tag">表情标签，如 [emoji_001]</param>
        /// <param name="spriteName">该帧的图片名称</param>
        /// <param name="frameIndex">帧索引 (从1开始)</param>
        /// <param name="raycastEnabled">是否启用射线检测</param>
        public static void RegisterEmoji(string tag, string spriteName, int frameIndex, bool raycastEnabled = true)
        {
            if (!s_emojiRegistry.TryGetValue(tag, out var data))
            {
                data = new EmojiAnimationData();
                data.RaycastEnabled = raycastEnabled;
                s_emojiRegistry[tag] = data;
            }

            while (data.FrameSprites.Count < frameIndex)
            {
                data.FrameSprites.Add(null);
            }

            if (frameIndex > 0 && frameIndex <= data.FrameSprites.Count)
            {
                data.FrameSprites[frameIndex - 1] = spriteName;
            }
            else
            {
                data.FrameSprites.Add(spriteName);
            }
        }

        /// <summary>
        /// 检查标签是否为已注册的表情 (Span 版本，避免字符串分配)
        /// </summary>
        public static bool IsEmojiSpan(ReadOnlySpan<char> tag)
        {
            foreach (var key in s_emojiRegistry.Keys)
            {
                if (tag.SequenceEqual(key.AsSpan()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取表情动画数据
        /// </summary>
        public static EmojiAnimationData GetEmojiData(string tag)
        {
            s_emojiRegistry.TryGetValue(tag, out var data);
            return data;
        }

        /// <summary>
        /// 清除所有已注册的表情
        /// </summary>
        public static void ClearAll()
        {
            s_emojiRegistry.Clear();
        }

        /// <summary>
        /// 使用项目的资源加载模块设置图片
        /// 直接使用图片名称通过 DGame.SetSpriteExtensions 加载
        /// </summary>
        /// <param name="image">目标 Image 组件</param>
        /// <param name="spriteName">图片名称 (直接作为资源地址使用)</param>
        /// <param name="setNativeSize">是否设置原始尺寸</param>
        /// <param name="callback">图片加载完成后的回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static void SetSprite(Image image, string spriteName, bool setNativeSize = false,
            Action<Image> callback = null, CancellationToken cancellationToken = default)
        {
            if (image == null || string.IsNullOrEmpty(spriteName))
            {
                return;
            }

            image.SetSprite(spriteName, setNativeSize, callback, cancellationToken);
        }
    }
}