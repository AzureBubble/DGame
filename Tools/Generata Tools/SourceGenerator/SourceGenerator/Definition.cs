using System.Collections.Generic;

namespace SourceGenerator;

public class Definition
{
    public static readonly List<string> TargetNameSpaces = ["DGame"];
    public static readonly string[] UsingNameSpace = ["UnityEngine", "UnityEngine.UI", "DGame"];//"UnityEngine", "UnityEngine.UI", "DGame"
    public const string NameSpace = "DGame";
    public const string AttributeName = "EventInterface";
    public const string StringToHash = "StringId.StringToHash";
}