using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Judgement;

[Generator]
public class ComponentBufferWriterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var synchronizedComponents = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                transform: (generatorSyntaxContext, cancellationToken) =>
                {
                    if (generatorSyntaxContext.Node is not ClassDeclarationSyntax classDeclarationSyntax)
                        return null; 
        
                    if (generatorSyntaxContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax,
                            cancellationToken) is not ITypeSymbol typedSymbol)
                        return null;

                    if (typedSymbol.IsAbstract)
                        return null;
                    
                    if (typedSymbol.BaseType is null)
                        return null;

                    if (typedSymbol.BaseType.Name != "Component")
                        return null;

                    var attributes = typedSymbol.GetAttributes().ToList();

                    if (attributes.Find(x => x.AttributeClass?.Name == "Synchronized") == null)
                        return null;

                    var properties = typedSymbol.GetMembers().ToList();

                    var containsSynchronizedField = false;
                    foreach (var propertyAttributes in properties.Select(property => property.GetAttributes().ToList()))
                    {
                        if (propertyAttributes.Find(x => x.AttributeClass?.Name == "SynchronizedField") == null)
                            continue;
                        
                        containsSynchronizedField = true;
                        break;
                    }

                    if (!containsSynchronizedField)
                        return null;
                    
                    return (typedSymbol);
                }).Where(typedSymbol => typedSymbol is not null).Collect();
        
        context.RegisterSourceOutput(synchronizedComponents, GenerateComponentBufferWriterFile);
    }

    private void GenerateComponentBufferWriterFile(SourceProductionContext context,
        ImmutableArray<ITypeSymbol?> components)
    {
        var sourceBuilder = new StringBuilder();
        
        List<string> usingNamespaces = [];
        foreach (var component in components)
        {
            if (component is null)
                continue;

            if (usingNamespaces.Contains(component.ContainingNamespace.ToDisplayString()))
                continue;
            
            usingNamespaces.Add(component.ContainingNamespace.ToDisplayString());
            sourceBuilder.AppendLine($"using {component.ContainingNamespace.ToDisplayString()};");
        }
        
        sourceBuilder.AppendLine(@"using Game.Temperance.NCS;

namespace Game.Temperance.Network;

public partial class ComponentBufferWriter
{");


        sourceBuilder.AppendLine(@"    public void WriteUnreliableForComponent<T>(ushort componentId, T component, ref BufferWriter bufferWriter) where T : ISynchronizedComponent
    {
        switch (componentId) {");
        foreach (var typedSymbol in components)
        {
            if (typedSymbol is null)
                continue;

            sourceBuilder.AppendLine($"            case {typedSymbol.Name}.ComponentId:");
            sourceBuilder.AppendLine(
                $"                 component.WriteUnreliableChannel(ref bufferWriter);");
            sourceBuilder.AppendLine("                 break;");
        }
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine(@"    public void ReadUnreliableForComponent(ref BufferReader bufferReader)
    {
        switch (componentId) {");

        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine(@"    public void WriteReliableForComponent(ref BufferWriter bufferWriter)
    {
        switch (componentId) {");

        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine(@"    public void ReadReliableForComponent(ref BufferReader bufferReader)
    {
        switch (componentId) {");

        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine("}");
        // context.AddSource($"ComponentBufferWriter.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }
}