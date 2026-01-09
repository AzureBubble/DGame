using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
namespace EventAnalyzer;

/// <summary>
/// 游戏事件分析器
/// <remarks>用于在编译时检测 事件监听方法 调用的泛型参数</remarks>
/// <remarks>是否与对应接口方法的参数类型一致</remarks>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GameEventAnalyzer : DiagnosticAnalyzer
{
    #region 诊断规则定义

    /// <summary>
    /// 参数类型不匹配规则
    /// <remarks>当泛型参数类型与接口方法参数类型不一致时触发</remarks>
    /// </summary>
    private static readonly DiagnosticDescriptor m_ruleTypeMismatch = new DiagnosticDescriptor(
        Definition.DiagnosticId, // 诊断ID（唯一标识符）
        Definition.Title, // 标题（简短描述）
        Definition.MessageFormat, // 错误消息模板（支持格式化参数）
        Definition.Category, // 类别（用于分组）
        DiagnosticSeverity.Error, // 严重级别
        isEnabledByDefault: true, // 是否默认启用
        description: Definition.Description); // 详细说明（可选）

    /// <summary>
    /// 参数数量不匹配规则
    /// <remarks>当泛型参数数量与接口方法参数数量不一致时触发</remarks>
    /// </summary>
    private static readonly DiagnosticDescriptor m_ruleParamCountMismatch = new DiagnosticDescriptor(
        Definition.DiagnosticId_ParamCount,
        Definition.TitleParamCount,
        Definition.MessageFormatParamCount,
        Definition.Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Definition.DescriptionParamCount);

    #endregion

    /// <summary>
    /// 返回此分析器支持的所有诊断规则
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(m_ruleTypeMismatch, m_ruleParamCountMismatch);

    /// <summary>
    /// 初始化分析器，注册语法节点分析回调
    /// </summary>
    /// <param name="context">分析上下文</param>
    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提高性能
        context.EnableConcurrentExecution();
        // 注册方法调用表达式的分析回调
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    /// <summary>
    /// 分析方法调用表达式
    /// </summary>
    /// <remarks>检测 事件监听方法 调用的泛型参数是否与接口方法参数匹配</remarks>
    /// <param name="context">语法节点分析上下文</param>
    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // 获取方法符号
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);

        if (!(symbolInfo.Symbol is IMethodSymbol methodSymbol))
        {
            return;
        }

        // 检查是否是 Definition 包含的事件监听调用方法
        if (!Definition.CheckMethodNameList.Contains(methodSymbol.Name))
        {
            return;
        }

        // 获取泛型参数
        var typeArguments = methodSymbol.TypeArguments;

        // 获取第一个参数（事件ID）
        var arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count == 0)
        {
            return;
        }

        var firstArg = arguments[0].Expression;

        // 解析事件ID参数，获取接口名和方法名
        if (!TryParseEventId(firstArg, context.SemanticModel, out var interfaceName, out var methodName,
                out var eventClassName))
        {
            return;
        }

        // 查找对应的接口
        var interfaceSymbol = FindInterface(context.Compilation, interfaceName, eventClassName);

        if (interfaceSymbol == null)
        {
            return;
        }

        // 查找对应的方法
        var interfaceMethod = interfaceSymbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (interfaceMethod == null)
        {
            return;
        }

        // 获取接口方法的参数类型
        var parameterTypes = interfaceMethod.Parameters.Select(p => p.Type).ToList();

        // 检查参数数量是否匹配
        if (typeArguments.Length != parameterTypes.Count)
        {
            var diagnostic = Diagnostic.Create(
                m_ruleParamCountMismatch, // 诊断规则描述符
                invocation.GetLocation(), // 错误位置
                typeArguments.Length, // 调用的参数数量
                interfaceName, // "ILoginUI"
                methodName, // "Test"
                parameterTypes.Count); // 原始方法参数数量

            context.ReportDiagnostic(diagnostic);
            return;
        }

        // 逐个比较参数类型
        for (int i = 0; i < typeArguments.Length; i++)
        {
            var actualType = typeArguments[i];
            var expectedType = parameterTypes[i];

            if (!SymbolEqualityComparer.Default.Equals(actualType, expectedType))
            {
                var diagnostic = Diagnostic.Create(
                    m_ruleTypeMismatch, // 诊断规则描述符
                    invocation.GetLocation(), // 错误位置
                    GetTypeName(actualType), // 实际的参数类型
                    interfaceName, // "ILoginUI"
                    methodName, // "Test"
                    GetTypeName(expectedType), // 期望的参数类型
                    i + 1); // 参数位置（从1开始）

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 解析事件ID参数，提取接口名和方法名
    /// <remarks>e.g.: ITestUI_Event.Test -> interfaceName="ITestUI", methodName="Test"</remarks>
    /// </summary>
    private bool TryParseEventId(ExpressionSyntax expression, SemanticModel semanticModel,
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
    private INamedTypeSymbol? FindInterface(Compilation compilation, string interfaceName, string eventClassName)
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
    private string GetTypeName(ITypeSymbol type)
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