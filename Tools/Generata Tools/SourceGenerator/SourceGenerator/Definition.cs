using System.Collections.Generic;

namespace SourceGenerator;

public class Definition
{
    /// <summary>
    /// 指定检测的命名空间
    /// </summary>
    public static readonly List<string> TargetNameSpaces = ["DGame"];
    public static readonly string[] UsingNameSpace = ["UnityEngine", "UnityEngine.UI", "DGame"];//"UnityEngine", "UnityEngine.UI", "DGame"

    /// <summary>
    /// 文件生成的命名空间
    /// </summary>
    public const string NameSpace = "DGame";
    public const string AttributeName = "EventInterface";
    public const string StringToHash = "StringId.StringToHash";
}