using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace EcsSystemGenerator;

/// <summary>
/// ECS 生命周期系统注册代码生成器。
/// </summary>
[Generator]
public sealed class EcsSystemGenerator : ISourceGenerator
{
    #region Initialize

    /// <summary>
    /// 初始化生成器。
    /// </summary>
    /// <param name="context">生成器初始化上下文。</param>
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    #endregion

    #region Execute

    /// <summary>
    /// 扫描 GameBattle 程序集内实现 GameBattle.EcsSystem.IEntitySystem 的生命周期系统，并生成注册代码。
    /// </summary>
    /// <param name="context">生成器执行上下文。</param>
    public void Execute(GeneratorExecutionContext context)
    {
        if (!string.Equals(context.Compilation.AssemblyName, Definition.TargetAssemblyName, StringComparison.Ordinal))
        {
            return;
        }

        var entitySystemInterface = context.Compilation.GetTypeByMetadataName(Definition.EcsEntitySystemInterfaceName);
        if (entitySystemInterface == null)
        {
            return;
        }

        var systemTypes = CollectSystemTypes(context.Compilation.GlobalNamespace, entitySystemInterface);
        context.AddSource("EntitySystem.g.cs", GenerateEntitySystemRegisterCode(systemTypes));
    }

    #endregion

    #region Collect

    /// <summary>
    /// 收集所有需要自动注册的外置生命周期系统类型。
    /// </summary>
    /// <param name="globalNamespace">编译全局命名空间。</param>
    /// <param name="entitySystemInterface">ECS 外置实体系统接口。</param>
    /// <returns>稳定排序后的系统类型列表。</returns>
    private static List<INamedTypeSymbol> CollectSystemTypes(
        INamespaceSymbol globalNamespace,
        INamedTypeSymbol entitySystemInterface)
    {
        var systemTypes = new List<INamedTypeSymbol>();
        CollectSystemTypes(globalNamespace, entitySystemInterface, systemTypes);

        return systemTypes
            .GroupBy(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .Select(group => group.First())
            .OrderBy(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 递归扫描命名空间成员。
    /// </summary>
    /// <param name="namespaceSymbol">当前命名空间。</param>
    /// <param name="entitySystemInterface">ECS 外置实体系统接口。</param>
    /// <param name="systemTypes">收集结果。</param>
    private static void CollectSystemTypes(
        INamespaceSymbol namespaceSymbol,
        INamedTypeSymbol entitySystemInterface,
        List<INamedTypeSymbol> systemTypes)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                if (IsTargetNamespace(childNamespace))
                {
                    CollectSystemTypes(childNamespace, entitySystemInterface, systemTypes);
                }

                continue;
            }

            if (member is INamedTypeSymbol typeSymbol)
            {
                CollectSystemTypes(typeSymbol, entitySystemInterface, systemTypes);
            }
        }
    }

    /// <summary>
    /// 递归扫描类型成员。
    /// </summary>
    /// <param name="typeSymbol">当前类型。</param>
    /// <param name="entitySystemInterface">ECS 外置实体系统接口。</param>
    /// <param name="systemTypes">收集结果。</param>
    private static void CollectSystemTypes(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol entitySystemInterface,
        List<INamedTypeSymbol> systemTypes)
    {
        if (IsRegisterableSystem(typeSymbol, entitySystemInterface))
        {
            systemTypes.Add(typeSymbol);
        }

        foreach (var nestedType in typeSymbol.GetTypeMembers())
        {
            CollectSystemTypes(nestedType, entitySystemInterface, systemTypes);
        }
    }

    /// <summary>
    /// 判断命名空间是否属于 GameBattle。
    /// </summary>
    /// <param name="namespaceSymbol">待检测命名空间。</param>
    /// <returns>属于目标命名空间时返回 true。</returns>
    private static bool IsTargetNamespace(INamespaceSymbol namespaceSymbol)
    {
        var namespaceName = namespaceSymbol.ToDisplayString();
        return namespaceName == Definition.TargetBattleNamespace
               || namespaceName.StartsWith(Definition.TargetBattleNamespace + ".", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断类型是否是可自动注册的外置生命周期系统。
    /// </summary>
    /// <param name="typeSymbol">待检测类型。</param>
    /// <param name="entitySystemInterface">ECS 外置实体系统接口。</param>
    /// <returns>可注册时返回 true。</returns>
    private static bool IsRegisterableSystem(INamedTypeSymbol typeSymbol, INamedTypeSymbol entitySystemInterface)
    {
        return typeSymbol.TypeKind == TypeKind.Class
               && !typeSymbol.IsAbstract
               && !typeSymbol.IsStatic
               && !typeSymbol.IsGenericType
               && IsTargetNamespace(typeSymbol.ContainingNamespace)
               && ImplementsEntitySystem(typeSymbol, entitySystemInterface)
               && HasAccessibleParameterlessConstructor(typeSymbol);
    }

    /// <summary>
    /// 判断类型是否实现 GameBattle.EcsSystem.IEntitySystem。
    /// </summary>
    /// <param name="typeSymbol">待检测类型。</param>
    /// <param name="entitySystemInterface">ECS 外置实体系统接口。</param>
    /// <returns>实现接口时返回 true。</returns>
    private static bool ImplementsEntitySystem(INamedTypeSymbol typeSymbol, INamedTypeSymbol entitySystemInterface)
    {
        foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceSymbol, entitySystemInterface))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断类型是否有当前程序集可访问的无参构造函数。
    /// </summary>
    /// <param name="typeSymbol">待检测类型。</param>
    /// <returns>存在可访问无参构造函数时返回 true。</returns>
    private static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol typeSymbol)
    {
        foreach (var constructor in typeSymbol.InstanceConstructors)
        {
            if (constructor.Parameters.Length != 0)
            {
                continue;
            }

            if (constructor.DeclaredAccessibility == Accessibility.Public
                || constructor.DeclaredAccessibility == Accessibility.Internal)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Generate

    /// <summary>
    /// 生成 EntitySystem 自动注册 partial 方法实现。
    /// </summary>
    /// <param name="systemTypes">需要注册的系统类型。</param>
    /// <returns>生成的 C# 源码。</returns>
    private static string GenerateEntitySystemRegisterCode(IReadOnlyList<INamedTypeSymbol> systemTypes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("//----------------------------------------------------------");
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// \tThis code was generated by the source generator.");
        sb.AppendLine("// \tChanges to this file may cause incorrect behavior.");
        sb.AppendLine("// \twill be lost if the code is regenerated.");
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("//----------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine("namespace GameBattle");
        sb.AppendLine("{");
        sb.AppendLine("\tpublic sealed partial class EntitySystem");
        sb.AppendLine("\t{");
        sb.AppendLine("\t\tpartial void RegisterGeneratedSystems()");
        sb.AppendLine("\t\t{");

        for (var i = 0; i < systemTypes.Count; i++)
        {
            var typeName = systemTypes[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"\t\t\tRegisterSystem(new {typeName}());");
        }

        sb.AppendLine("\t\t}");
        sb.AppendLine("\t}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    #endregion
}
