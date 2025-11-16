using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [Serializable]
    public class UITextLocalizationExtend
    {
        // [SerializeField] private bool m_isUseBestFit;
        //
        // [SerializeField] private int m_visibleLines;
        //
        // /// <summary>
        // /// 当前可见的文字行数
        // /// </summary>
        // public int VisibleLines => m_visibleLines;
        //
        // private readonly UIVertex[] m_tmpVerts = new UIVertex[4];
        //
        // public void OnPopulateMesh(VertexHelper toFill, Text text)
        // {
        //     if (!m_isUseBestFit)
        //     {
        //         return;
        //     }
        // }
        //
        // private bool OverrideForBestFit(VertexHelper toFill, Text text)
        // {
        //     if (null == text.font) return false;
        //     text.m_DisableFontTextureRebuiltCallback = true;
        //     UseFitSettings();
        //     IList<UIVertex> verts = text.cachedTextGenerator.verts;
        //     float unitsPerPixel = 1 / text.pixelsPerUnit;
        //     int vertCount = verts.Count;
        //     // 没有要处理的对象时，直接return。
        //     if (vertCount <= 0)
        //     {
        //         toFill.Clear();
        //         return false;
        //     }
        //     Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
        //     roundingOffset = text.PixelAdjustPoint(roundingOffset) - roundingOffset;
        //     toFill.Clear();
        //
        //     for (int i = 0; i < vertCount; ++i)
        //     {
        //         int tempVertsIndex = i & 3;
        //         m_tmpVerts[tempVertsIndex] = verts[i];
        //         m_tmpVerts[tempVertsIndex].position *= unitsPerPixel;
        //         if (roundingOffset != Vector2.zero)
        //         {
        //             m_tmpVerts[tempVertsIndex].position.x += roundingOffset.x;
        //             m_tmpVerts[tempVertsIndex].position.y += roundingOffset.y;
        //         }
        //
        //         if (tempVertsIndex == 3)
        //         {
        //             toFill.AddUIVertexQuad(m_tmpVerts);
        //         }
        //     }
        //     m_DisableFontTextureRebuiltCallback = false;
        //     VisibleLines = text.cachedTextGenerator.lineCount;
        //     return true;
        // }
        //
        // private void UseFitSettings()
        // {
        //     TextGenerationSettings settings = GetGenerationSettings(rectTransform.rect.size);
        //     settings.resizeTextForBestFit = false;
        //
        //     if (!text.resizeTextForBestFit)
        //     {
        //         cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
        //         return;
        //     }
        //
        //     int minSize = resizeTextMinSize;
        //     int txtLen = text.Length;
        //
        //     //从Best Fit中最大的值开始，逐次递减，每次减小后都尝试生成文本，
        //     //如果生成的文本可见字符数等于文本内容的长度，则找到满足需求(可以使所有文本都可见的最大字号)的字号。
        //     for (int i = resizeTextMaxSize; i >= minSize; --i)
        //     {
        //         settings.fontSize = i;
        //         cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
        //         if (cachedTextGenerator.characterCountVisible == txtLen) break;
        //     }
        // }
    }
}