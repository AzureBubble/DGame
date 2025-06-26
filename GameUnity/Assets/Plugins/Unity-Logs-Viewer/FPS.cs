using UnityEngine;
using System.Collections;

public class FPS : MonoBehaviour
{
    float deltaTime = 0.0f;

    GUIStyle mStyle;
    void Awake()
    {
        mStyle = new GUIStyle();
        mStyle.alignment = TextAnchor.UpperLeft;
        mStyle.normal.background = null;
        mStyle.fontSize = 35;
        mStyle.normal.textColor = Color.red;
    }

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {

        Rect rect = new Rect(0, 0, 500, 300);
        float fps = 1.0f / deltaTime;
        string text = string.Format(" FPS:{0:N0} ", fps);
        GUI.Label(rect, text, mStyle);

        Rect appInfoRect = new Rect(Screen.width - 400, Screen.height - 30, 500, 300);
    }
}