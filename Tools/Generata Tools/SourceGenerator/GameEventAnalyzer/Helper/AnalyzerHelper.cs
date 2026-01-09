using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EventAnalyzer;

public static class AnalyzerHelper
{
    /// <summary>
    /// 解析事件ID参数，提取接口名和方法名
    /// <remarks>e.g.: ITestUI_Event.Test -> interfaceName="ITestUI", methodName="Test"</remarks>
    /// </summary>
    public static bool TryParseEventId(ExpressionSyntax expression, SemanticModel semanticModel,
        out string interfaceName, out string methodName, out string eventClassName)
    {
        interfaceName = string.Empty;
        methodName = string.Empty;
        eventClassName = string.Empty;

        // 处理 ITestUI_Event.Test 这种成员访问表达式
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            // 获取成员名（方法名） Test
            methodName = memberAccess.Name.Identifier.Text;

            // 获取类名 ITestUI_Event
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                eventClassName = typeSymbol.Name;

                // 从 ITestUI_Event 推导出 ITestUI
                if (eventClassName.EndsWith(Definition.EventClassNameEndsWith))
                {
                    interfaceName = eventClassName.Substring(0,
                        eventClassName.Length - Definition.EventClassNameEndsWith.Length);
                    return true;
                }
            }
            else if (memberAccess.Expression is IdentifierNameSyntax identifier)
            {
                eventClassName = identifier.Identifier.Text;

                // 从 ITestUI_Event 推导出 ITestUI
                if (eventClassName.EndsWith(Definition.EventClassNameEndsWith))
                {
                    interfaceName = eventClassName.Substring(0,
                        eventClassName.Length - Definition.EventClassNameEndsWith.Length);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 查找对应的接口类型
    /// <remarks>先尝试常见命名空间，再遍历所有语法树查找</remarks>
    /// </summary>
    /// <param name="compilation">编译对象</param>
    /// <param name="interfaceName">接口名称（如 ITestUI）</param>
    /// <param name="eventClassName">事件类名称（如 ITestUI_Event）</param>
    /// <returns>找到的接口符号，未找到返回 null</returns>
    public static INamedTypeSymbol? FindInterface(Compilation compilation, string interfaceName, string eventClassName)
    {
        foreach (var ns in Definition.CommonNamespaces)
        {
            var fullName = string.IsNullOrEmpty(ns) ? interfaceName : $"{ns}.{interfaceName}";
            var symbol = compilation.GetTypeByMetadataName(fullName);

            if (symbol != null && symbol.TypeKind == TypeKind.Interface)
            {
                return symbol;
            }
        }

        // 遍历所有语法树查找接口
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();

            // 查找接口声明
            var interfaces = root.DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .Where(i => i.Identifier.Text == interfaceName);

            foreach (var interfaceDecl in interfaces)
            {
                // 获取命名空间
                var namespaceDecl = interfaceDecl.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault();

                string? namespaceName = namespaceDecl?.Name.ToString();

                if (namespaceName != null)
                {
                    var fullInterfaceName = $"{namespaceName}.{interfaceName}";
                    var interfaceSymbol = compilation.GetTypeByMetadataName(fullInterfaceName);

                    if (interfaceSymbol != null && interfaceSymbol.TypeKind == TypeKind.Interface)
                    {
                        return interfaceSymbol;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 获取类型的显示名称
    /// <remarks>将系统类型转换为 C# 关键字别名（e.g. System.Int32 -> int）</remarks>
    /// </summary>
    /// <param name="type">类型符号</param>
    /// <returns>类型的显示名称</returns>
    public static string GetTypeName(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Int32:
                return "int";

            case SpecialType.System_String:
                return "string";

            case SpecialType.System_Boolean:
                return "bool";

            case SpecialType.System_Single:
                return "float";

            case SpecialType.System_Double:
                return "double";

            case SpecialType.System_Int64:
                return "long";

            case SpecialType.System_Byte:
                return "byte";

            case SpecialType.System_Char:
                return "char";

            case SpecialType.System_Object:
                return "object";

            default:
                return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }
}