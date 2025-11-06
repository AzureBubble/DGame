using UnityEngine;
using System.IO;
using System.Collections;
using DGame;

#if UNITY_EDITOR

using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEditor.Callbacks;

public class ReporterEditor : Editor
{
	[MenuItem("DGame Tools/日志系统/关闭整个日志系统", false, 80)]
	public static void DisableLogSystem()
	{
		DGame.ScriptingDefineSymbolsTools.DisableAllLogs();
		DisableLog2File();
		DisableFPS();
		DisableReporter();
		AssetDatabase.SaveAssets();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
		AssetDatabase.Refresh();
	}

	#region 日志打印系统

	/// <summary>
	/// 开启日志打印到本地系统功能 同时开启所有级别的日志输出
	/// </summary>
	[Tooltip("开启日志打印系统会默认开启所有级别的日志输出宏")]
	[MenuItem("DGame Tools/日志系统/开启日志打印系统", false, 20)]
	public static void EnableLog2File()
	{
		var logObj = GameObject.Find("LogRoot");

		if (logObj != null)
		{
			logObj.AddComponent<DGameLog2File>();
		}
		else
		{
			logObj = new GameObject("LogRoot");
			logObj.AddComponent<LogRoot>();
			logObj.AddComponent<DGameLog2File>();
		}

		DGame.ScriptingDefineSymbolsTools.EnableAllLogs();
		AssetDatabase.SaveAssets();
		EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
		AssetDatabase.Refresh();
	}

	/// <summary>
	/// 关闭日志打印到本地系统功能
	/// </summary>
	[Tooltip("关闭日志打印系统不会禁用任何级别的日志输出宏")]
	[MenuItem("DGame Tools/日志系统/禁用日志打印系统", false, 21)]
	public static void DisableLog2File()
	{
		var log2File = GameObject.FindObjectOfType<DGameLog2File>();

		if (log2File != null)
		{
			DestroyImmediate(log2File);
			RemoveLogRootObj();
			AssetDatabase.SaveAssets();
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			AssetDatabase.Refresh();
		}
	}

	#endregion

	#region FPS

	/// <summary>
	/// 开启FPS显示
	/// </summary>
	[MenuItem("DGame Tools/日志系统/开启FPS", false, 62)]
	public static void EnableFPS()
	{
		var fpsObj = GameObject.Find("FPS");

		if (fpsObj == null)
		{
			fpsObj = new GameObject("FPS");
			fpsObj.AddComponent<FPS>();
			var logObj = GameObject.Find("LogRoot");

			if (logObj != null)
			{
				fpsObj.transform.SetParent(logObj.transform);
			}
			else
			{
				logObj = new GameObject("LogRoot");
				logObj.AddComponent<LogRoot>();
				fpsObj.transform.SetParent(logObj.transform);
			}

			AssetDatabase.SaveAssets();
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			AssetDatabase.Refresh();
		}
	}

	/// <summary>
	/// 开启FPS显示
	/// </summary>
	[MenuItem("DGame Tools/日志系统/关闭FPS", false, 63)]
	public static void DisableFPS()
	{
		var fpsObj = GameObject.Find("FPS");

		if (fpsObj != null)
		{
			DestroyImmediate(fpsObj);
			RemoveLogRootObj();
			AssetDatabase.SaveAssets();
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			AssetDatabase.Refresh();
		}
	}

	#endregion

	#region Reporter

	/// <summary>
	/// 开启Reporter日志窗口系统
	/// </summary>
	[MenuItem("DGame Tools/日志系统/开启Reporter日志窗口系统", false, 60)]
	public static void EnableReporter()
	{
		var reporterObj = GameObject.Find("Reporter");

		if (reporterObj == null)
		{
			CreateReporter();
			AssetDatabase.SaveAssets();
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			AssetDatabase.Refresh();
		}
	}

	/// <summary>
	/// 关闭Reporter日志窗口系统
	/// </summary>
	[MenuItem("DGame Tools/日志系统/关闭Reporter日志窗口系统", false, 61)]
	public static void DisableReporter()
	{
		var reporterObj = GameObject.Find("Reporter");

		if (reporterObj != null)
		{
			DestroyImmediate(reporterObj);
			RemoveLogRootObj();
			AssetDatabase.SaveAssets();
			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			AssetDatabase.Refresh();
		}
	}

	#endregion

	private static void RemoveLogRootObj()
	{
		var logObj = GameObject.Find("LogRoot");

		if (logObj != null && logObj.transform.childCount <= 0)
		{
			DestroyImmediate(logObj);
		}
	}

	// [MenuItem("Reporter/Create")]
	public static void CreateReporter()
	{
		const int ReporterExecOrder = -12000;
		GameObject reporterObj = new GameObject();
		reporterObj.name = "Reporter";
		Reporter reporter = reporterObj.AddComponent<Reporter>();
		reporterObj.AddComponent<ReporterMessageReceiver>();
		var logObj = GameObject.Find("LogRoot");

		if (logObj != null)
		{
			reporterObj.transform.SetParent(logObj.transform);
		}
		else
		{
			logObj = new GameObject("LogRoot");
			logObj.AddComponent<LogRoot>();
			reporterObj.transform.SetParent(logObj.transform);
		}
		//reporterObj.AddComponent<TestReporter>();

		// Register root object for undo.
		Undo.RegisterCreatedObjectUndo(reporterObj, "Create Reporter Object");

		MonoScript reporterScript = MonoScript.FromMonoBehaviour(reporter);
		string reporterPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(reporterScript));

		if (MonoImporter.GetExecutionOrder(reporterScript) != ReporterExecOrder)
		{
			MonoImporter.SetExecutionOrder(reporterScript, ReporterExecOrder);
			//Debug.Log("Fixing exec order for " + reporterScript.name);
		}

		reporter.images = new Images();
		reporter.images.clearImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/clear.png"), typeof(Texture2D));
		reporter.images.collapseImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/collapse.png"),
				typeof(Texture2D));
		reporter.images.clearOnNewSceneImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/clearOnSceneLoaded.png"),
				typeof(Texture2D));
		reporter.images.showTimeImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/timer_1.png"),
				typeof(Texture2D));
		reporter.images.showSceneImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/UnityIcon.png"),
				typeof(Texture2D));
		reporter.images.userImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/user.png"), typeof(Texture2D));
		reporter.images.showMemoryImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/memory.png"),
				typeof(Texture2D));
		reporter.images.softwareImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/software.png"),
				typeof(Texture2D));
		reporter.images.dateImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/date.png"), typeof(Texture2D));
		reporter.images.showFpsImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/fps.png"), typeof(Texture2D));
		//reporter.images.graphImage           = (Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/chart.png"), typeof(Texture2D));
		reporter.images.infoImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/info.png"), typeof(Texture2D));
		reporter.images.saveLogsImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/Save.png"), typeof(Texture2D));
		reporter.images.searchImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/search.png"),
				typeof(Texture2D));
		reporter.images.copyImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/copy.png"), typeof(Texture2D));
		reporter.images.closeImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/close.png"), typeof(Texture2D));
		reporter.images.buildFromImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/buildFrom.png"),
				typeof(Texture2D));
		reporter.images.systemInfoImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/ComputerIcon.png"),
				typeof(Texture2D));
		reporter.images.graphicsInfoImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/graphicCard.png"),
				typeof(Texture2D));
		reporter.images.backImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/back.png"), typeof(Texture2D));
		reporter.images.logImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/log_icon.png"),
				typeof(Texture2D));
		reporter.images.warningImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/warning_icon.png"),
				typeof(Texture2D));
		reporter.images.errorImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/error_icon.png"),
				typeof(Texture2D));
		reporter.images.barImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/bar.png"), typeof(Texture2D));
		reporter.images.button_activeImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/button_active.png"),
				typeof(Texture2D));
		reporter.images.even_logImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/even_log.png"),
				typeof(Texture2D));
		reporter.images.odd_logImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/odd_log.png"),
				typeof(Texture2D));
		reporter.images.selectedImage =
			(Texture2D)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/selected.png"),
				typeof(Texture2D));

		reporter.images.reporterScrollerSkin =
			(GUISkin)AssetDatabase.LoadAssetAtPath(Path.Combine(reporterPath, "Images/reporterScrollerSkin.guiskin"),
				typeof(GUISkin));
	}
}

public class ReporterModificationProcessor : UnityEditor.AssetModificationProcessor
{
	[InitializeOnLoad]
	public class BuildInfo
	{
		static BuildInfo()
		{
			EditorApplication.update += Update;
		}

		static bool isCompiling = true;
		static void Update()
		{
          
			if (!EditorApplication.isCompiling && isCompiling) {
				//Debug.Log("Finish Compile");
				if (!Directory.Exists(Application.dataPath + "/StreamingAssets")) {
					Directory.CreateDirectory(Application.dataPath + "/StreamingAssets");
				}
				string info_path = Application.dataPath + "/StreamingAssets/build_info"; 
				StreamWriter build_info = new StreamWriter(info_path);
				build_info.Write("Build from " + SystemInfo.deviceName + " at " + System.DateTime.Now.ToString());
				build_info.Close();
			}

			isCompiling = EditorApplication.isCompiling;
		}
	}
}

#endif