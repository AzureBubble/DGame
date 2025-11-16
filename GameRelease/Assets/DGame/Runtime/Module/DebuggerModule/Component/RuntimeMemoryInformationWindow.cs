using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

namespace DGame
{
    public partial class DebuggerDriver
    {
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase
            where T : UnityEngine.Object
        {
            private readonly List<Sample> m_samples = new List<Sample>();
            private readonly Comparison<Sample> m_sampleComparer = SampleComparer;
            private DateTime m_sampleTime = DateTime.MinValue;
            private long m_sampleSize = 0L;
            private long m_duplicateSampleSize = 0L;
            private int m_duplicateSimpleCount = 0;

            protected override void OnDrawScrollableWindow()
            {
                string typeName = typeof(T).Name;
                GUILayout.Label(Utility.StringUtil.Format("<b>{0} Runtime Memory Information</b>", typeName));
                GUILayout.BeginVertical("box");
                {
                    if (GUILayout.Button(Utility.StringUtil.Format("Take Sample for {0}", typeName),
                            GUILayout.Height(30f)))
                    {
                        TakeSample();
                    }

                    if (m_sampleTime <= DateTime.MinValue)
                    {
                        GUILayout.Label(Utility.StringUtil.Format("<b>Please take sample for {0} first.</b>",
                            typeName));
                    }
                    else
                    {
                        if (m_duplicateSimpleCount > 0)
                        {
                            GUILayout.Label(Utility.StringUtil.Format(
                                "<b>{0} {1}s ({2}) obtained at {3:yyyy-MM-dd HH:mm:ss}, while {4} {1}s ({5}) might be duplicated.</b>",
                                m_samples.Count, typeName, GetByteLengthString(m_sampleSize),
                                m_sampleTime.ToLocalTime(), m_duplicateSimpleCount,
                                GetByteLengthString(m_duplicateSampleSize)));
                        }
                        else
                        {
                            GUILayout.Label(Utility.StringUtil.Format(
                                "<b>{0} {1}s ({2}) obtained at {3:yyyy-MM-dd HH:mm:ss}.</b>", m_samples.Count, typeName,
                                GetByteLengthString(m_sampleSize), m_sampleTime.ToLocalTime()));
                        }

                        if (m_samples.Count > 0)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(Utility.StringUtil.Format("<b>{0} Name</b>", typeName));
                                GUILayout.Label("<b>Type</b>", GUILayout.Width(240f));
                                GUILayout.Label("<b>Size</b>", GUILayout.Width(80f));
                            }
                            GUILayout.EndHorizontal();
                        }

                        int count = 0;

                        for (int i = 0; i < m_samples.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(m_samples[i].Highlight
                                    ? Utility.StringUtil.Format("<color=yellow>{0}</color>", m_samples[i].Name)
                                    : m_samples[i].Name);
                                GUILayout.Label(m_samples[i].Highlight
                                        ? Utility.StringUtil.Format("<color=yellow>{0}</color>", m_samples[i].Type)
                                        : m_samples[i].Type, GUILayout.Width(240f));
                                GUILayout.Label(m_samples[i].Highlight
                                        ? Utility.StringUtil.Format("<color=yellow>{0}</color>",
                                            GetByteLengthString(m_samples[i].Size))
                                        : GetByteLengthString(m_samples[i].Size), GUILayout.Width(80f));
                            }
                            GUILayout.EndHorizontal();

                            count++;

                            if (count >= Constant.SHOW_SAMPLE_COUNT)
                            {
                                break;
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }

            private void TakeSample()
            {
                m_sampleTime = DateTime.UtcNow;
                m_sampleSize = 0L;
                m_duplicateSampleSize = 0L;
                m_duplicateSimpleCount = 0;
                m_samples.Clear();

                T[] samples = Resources.FindObjectsOfTypeAll<T>();

                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
#if UNITY_5_6_OR_NEWER
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
#else
                    sampleSize = Profiler.GetRuntimeMemorySize(samples[i]);
#endif
                    m_sampleSize += sampleSize;
                    m_samples.Add(new Sample(samples[i].name, samples[i].GetType().Name, sampleSize));
                }

                m_samples.Sort(m_sampleComparer);

                for (int i = 1; i < m_samples.Count; i++)
                {
                    if (m_samples[i].Name == m_samples[i - 1].Name && m_samples[i].Type == m_samples[i - 1].Type &&
                        m_samples[i].Size == m_samples[i - 1].Size)
                    {
                        m_samples[i].Highlight = true;
                        m_duplicateSampleSize += m_samples[i].Size;
                        m_duplicateSimpleCount++;
                    }
                }
            }

            private static int SampleComparer(Sample a, Sample b)
            {
                int result = b.Size.CompareTo(a.Size);

                if (result != 0)
                {
                    return result;
                }

                result = String.Compare(a.Type, b.Type, StringComparison.Ordinal);

                if (result != 0)
                {
                    return result;
                }

                return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
            }

            private sealed class Sample
            {
                private readonly string m_name;
                public string Name => m_name;
                private readonly string m_type;
                public string Type => m_type;
                private readonly long m_size;
                public long Size => m_size;

                public bool Highlight { get; set; }

                public Sample(string name, string type, long size)
                {
                    m_name = name;
                    m_type = type;
                    m_size = size;
                    Highlight = false;
                }
            }
        }
    }
}