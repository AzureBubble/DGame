using System.Collections.Generic;

namespace SourceGenerator;

public class Definition
{
    public static readonly List<string> TargetNameSpaces = ["GameLogic"];
    public static readonly string[] UsingNameSpace = [];//"UnityEngine", "UnityEngine.UI", "QFramework"
    public const string NameSpace = "GameLogic";
    public const string AttributeName = "EventInterface";
    public const string StringToHash = "RuntimeId.ToRuntimeId";
}