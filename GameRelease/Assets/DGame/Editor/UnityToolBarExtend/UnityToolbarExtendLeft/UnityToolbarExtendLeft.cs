using UnityEditor;

namespace DGame
{
    [InitializeOnLoad]
    public partial class UnityToolbarExtendLeft
    {
        static UnityToolbarExtendLeft()
        {
            UnityToolbarExtend.LEFT_TOOLBAR_GUI.Add(OnToolbarGUI_ScreenLauncher);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuit;
        }
    }
}