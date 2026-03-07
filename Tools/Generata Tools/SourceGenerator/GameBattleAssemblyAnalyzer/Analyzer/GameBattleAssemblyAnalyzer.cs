using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GameBattleAssemblyAnalyzer;

/// <summary>
/// GameBattle程序集分析器
/// <remarks>用于检测GameBattle程序集中是否使用了Unity引擎相关的代码</remarks>
/// <remarks>GameBattle是纯逻辑层程序集，不应该依赖Unity引擎的表现层代码</remarks>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GameBattleAssemblyAnalyzer : DiagnosticAnalyzer
{
    #region 诊断规则定义

    /// <summary>
    /// Unity引擎类型使用检测规则
    /// <remarks>当在GameBattle程序集中使用Unity引擎相关类型时触发</remarks>
    /// </summary>
    private static readonly DiagnosticDescriptor m_ruleUnityTypeUsage = new DiagnosticDescriptor(
        Definition.DiagnosticId_UnityTypeUsage,
        Definition.TitleUnityTypeUsage,
        Definition.MessageFormatUnityTypeUsage,
        Definition.Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Definition.DescriptionUnityTypeUsage);

    #endregion

    /// <summary>
    /// 返回此分析器支持的所有诊断规则
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(m_ruleUnityTypeUsage);

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

        // 注册多种语法节点的分析回调，以检测不同类型的Unity类型使用
        // 注意：避免重复检测，不注册VariableDeclaration（会被FieldDeclaration覆盖）
        // 不注册IdentifierName（太宽泛，会被其他具体回调覆盖）
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.CastExpression);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.AsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.IsExpression);
        context.RegisterSyntaxNodeAction(AnalyzeTypeUsage, SyntaxKind.GenericName);
    }

    /// <summary>
    /// 分析类型使用
    /// <remarks>检测是否使用了Unity引擎相关的类型</remarks>
    /// </summary>
    /// <param name="context">语法节点分析上下文</param>
    private void AnalyzeTypeUsage(SyntaxNodeAnalysisContext context)
    {
        // 检查当前代码是否在目标程序集(GameBattle)中
        if (!IsInTargetAssembly(context))
        {
            return;
        }

        // 根据不同的语法节点类型进行不同的分析
        switch (context.Node.Kind())
        {
            case SyntaxKind.UsingDirective:
                AnalyzeUsingDirective(context, (UsingDirectiveSyntax)context.Node);
                break;

            case SyntaxKind.FieldDeclaration:
                AnalyzeFieldDeclaration(context, (FieldDeclarationSyntax)context.Node);
                break;

            case SyntaxKind.PropertyDeclaration:
                AnalyzePropertyDeclaration(context, (PropertyDeclarationSyntax)context.Node);
                break;

            case SyntaxKind.Parameter:
                AnalyzeParameter(context, (ParameterSyntax)context.Node);
                break;

            case SyntaxKind.MethodDeclaration:
                AnalyzeMethodDeclaration(context, (MethodDeclarationSyntax)context.Node);
                break;

            case SyntaxKind.ObjectCreationExpression:
                AnalyzeObjectCreation(context, (ObjectCreationExpressionSyntax)context.Node);
                break;

            case SyntaxKind.CastExpression:
                AnalyzeCastExpression(context, (CastExpressionSyntax)context.Node);
                break;

            case SyntaxKind.AsExpression:
                AnalyzeAsExpression(context, (BinaryExpressionSyntax)context.Node);
                break;

            case SyntaxKind.IsExpression:
                AnalyzeIsExpression(context, (BinaryExpressionSyntax)context.Node);
                break;

            case SyntaxKind.GenericName:
                AnalyzeGenericName(context, (GenericNameSyntax)context.Node);
                break;
        }
    }

    /// <summary>
    /// 检查当前代码是否在目标程序集中
    /// </summary>
    /// <param name="context">语法节点分析上下文</param>
    /// <returns>是否在目标程序集中</returns>
    private bool IsInTargetAssembly(SyntaxNodeAnalysisContext context)
    {
        // 获取包含当前节点的符号
        var symbol = context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart);

        if (symbol == null)
        {
            return false;
        }

        // 获取符号所在的程序集
        var assembly = symbol.ContainingAssembly;

        if (assembly == null)
        {
            return false;
        }

        // 检查是否是目标程序集
        return assembly.Name == Definition.TargetAssemblyName;
    }

    /// <summary>
    /// 分析using指令
    /// </summary>
    private void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context, UsingDirectiveSyntax usingDirective)
    {
        var namespaceName = usingDirective.Name.ToString();

        // 检查是否使用了Unity命名空间
        if (IsUnityNamespace(namespaceName))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                usingDirective.GetLocation(),
                $"namespace {namespaceName}");

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析字段声明
    /// </summary>
    private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context, FieldDeclarationSyntax fieldDeclaration)
    {
        var typeSymbol = context.SemanticModel.GetTypeInfo(fieldDeclaration.Declaration.Type).Type;

        if (typeSymbol != null && IsUnityType(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                fieldDeclaration.Declaration.Type.GetLocation(),
                GetFullTypeName(typeSymbol));

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析属性声明
    /// </summary>
    private void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var typeSymbol = context.SemanticModel.GetTypeInfo(propertyDeclaration.Type).Type;

        if (typeSymbol != null && IsUnityType(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                propertyDeclaration.Type.GetLocation(),
                GetFullTypeName(typeSymbol));

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析参数声明
    /// </summary>
    private void AnalyzeParameter(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
    {
        if (parameter.Type != null)
        {
            var typeSymbol = context.SemanticModel.GetTypeInfo(parameter.Type).Type;

            if (typeSymbol != null && IsUnityType(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    m_ruleUnityTypeUsage,
                    parameter.Type.GetLocation(),
                    GetFullTypeName(typeSymbol));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 分析方法声明（返回类型）
    /// </summary>
    private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.ReturnType != null)
        {
            var typeSymbol = context.SemanticModel.GetTypeInfo(methodDeclaration.ReturnType).Type;

            if (typeSymbol != null && IsUnityType(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    m_ruleUnityTypeUsage,
                    methodDeclaration.ReturnType.GetLocation(),
                    GetFullTypeName(typeSymbol));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 分析对象创建表达式
    /// </summary>
    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
    {
        var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;

        if (typeSymbol != null && IsUnityType(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                objectCreation.GetLocation(),
                GetFullTypeName(typeSymbol));

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析类型转换表达式
    /// </summary>
    private void AnalyzeCastExpression(SyntaxNodeAnalysisContext context, CastExpressionSyntax castExpression)
    {
        var typeSymbol = context.SemanticModel.GetTypeInfo(castExpression.Type).Type;

        if (typeSymbol != null && IsUnityType(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                castExpression.Type.GetLocation(),
                GetFullTypeName(typeSymbol));

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 分析as表达式
    /// </summary>
    private void AnalyzeAsExpression(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax asExpression)
    {
        if (asExpression.Right != null)
        {
            var right = asExpression.Right;
            var typeSymbol = context.SemanticModel.GetTypeInfo(right).Type;

            if (typeSymbol != null && IsUnityType(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    m_ruleUnityTypeUsage,
                    right.GetLocation(),
                    GetFullTypeName(typeSymbol));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 分析is表达式
    /// </summary>
    private void AnalyzeIsExpression(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax isExpression)
    {
        if (isExpression.Right != null)
        {
            var right = isExpression.Right;
            var typeSymbol = context.SemanticModel.GetTypeInfo(right).Type;

            if (typeSymbol != null && IsUnityType(typeSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    m_ruleUnityTypeUsage,
                    right.GetLocation(),
                    GetFullTypeName(typeSymbol));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// 分析泛型名称
    /// </summary>
    private void AnalyzeGenericName(SyntaxNodeAnalysisContext context, GenericNameSyntax genericName)
    {
        var typeSymbol = context.SemanticModel.GetTypeInfo(genericName).Type;

        if (typeSymbol != null && IsUnityType(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(
                m_ruleUnityTypeUsage,
                genericName.GetLocation(),
                GetFullTypeName(typeSymbol));

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 检查命名空间是否是Unity命名空间
    /// </summary>
    /// <param name="namespaceName">命名空间名称</param>
    /// <returns>是否是Unity命名空间</returns>
    private bool IsUnityNamespace(string namespaceName)
    {
        return Definition.UnityNamespacePrefixes.Any(prefix =>
            namespaceName.StartsWith(prefix, System.StringComparison.Ordinal));
    }

    /// <summary>
    /// 检查类型是否是Unity类型
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>是否是Unity类型</returns>
    private bool IsUnityType(ITypeSymbol typeSymbol)
    {
        // 检查类型命名空间
        if (typeSymbol.ContainingNamespace != null)
        {
            var namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();

            if (IsUnityNamespace(namespaceName))
            {
                return true;
            }
        }

        // 检查类型名称是否在Unity类型列表中
        if (Definition.UnityTypeNames.Contains(typeSymbol.Name))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取类型的完整名称
    /// </summary>
    /// <param name="typeSymbol">类型符号</param>
    /// <returns>类型的完整名称</returns>
    private string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
