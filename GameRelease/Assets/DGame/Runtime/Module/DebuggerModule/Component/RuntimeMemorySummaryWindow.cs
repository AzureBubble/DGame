using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private sealed class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            private readonly List<Record> m_records = new List<Record>();
            private readonly Comparison<Record> m_recordComparer = RecordComparer;
            private DateTime m_sampleTime = DateTime.MinValue;
            private int m_sampleCount = 0;
            private long m_sampleSize = 0L;

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Runtime Memory Summary</b>");
                GUILayout.BeginVertical("box");
                {
                    if (GUILayout.Button("Take Sample", GUILayout.Height(30f)))
                    {
                        TakeSample();
                    }

                    if (m_sampleTime <= DateTime.MinValue)
                    {
                        GUILayout.Label("<b>Please take sample first.</b>");
                    }
                    else
                    {
                        GUILayout.Label(Utility.StringUtil.Format(
                            "<b>{0} Objects ({1}) obtained at {2:yyyy-MM-dd HH:mm:ss}</b>", m_sampleCount,
                            GetByteLengthString(m_sampleSize), m_sampleTime.ToLocalTime()));

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("<b>Type</b>");
                            GUILayout.Label("<b>Count</b>", GUILayout.Width(120f));
                            GUILayout.Label("<b>Size</b>", GUILayout.Width(120f));
                        }
                        GUILayout.EndHorizontal();

                        for (int i = 0; i < m_records.Count; i++)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(m_records[i].Name);
                                GUILayout.Label(m_records[i].Count.ToString(), GUILayout.Width(120f));
                                GUILayout.Label(GetByteLengthString(m_records[i].Size), GUILayout.Width(120f));
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
            }

            private void TakeSample()
            {
                m_records.Clear();
                m_sampleTime = DateTime.UtcNow;
                m_sampleCount = 0;
                m_sampleSize = 0L;

                UnityEngine.Object[] samples = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();

                for (int i = 0; i < samples.Length; i++)
                {
                    long sampleSize = 0L;
#if UNITY_5_6_OR_NEWER
                    sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
#else
                    sampleSize = Profiler.GetRuntimeMemorySize(samples[i]);
#endif
                    string name = samples[i].GetType().Name;
                    m_sampleCount++;
                    m_sampleSize += sampleSize;

                    Record record = null;

                    foreach (Record r in m_records)
                    {
                        if (r.Name == name)
                        {
                            record = r;
                            break;
                        }
                    }

                    if (record == null)
                    {
                        record = new Record(name);
                        m_records.Add(record);
                    }

                    record.Count++;
                    record.Size += sampleSize;
                }

                m_records.Sort(m_recordComparer);
            }

            private static int RecordComparer(Record a, Record b)
            {
                int result = b.Size.CompareTo(a.Size);

                if (result != 0)
                {
                    return result;
                }

                result = a.Count.CompareTo(b.Count);

                if (result != 0)
                {
                    return result;
                }

                return String.Compare(a.Name, b.Name, StringComparison.Ordinal);
            }
        }

        private sealed class Record
        {
            private readonly string m_name;
            public string Name => m_name;
            public int Count { get; set; }

            public long Size { get; set; }

            public Record(string name)
            {
                m_name = name;
            }
        }
    }
}