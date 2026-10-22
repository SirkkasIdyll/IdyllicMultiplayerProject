using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Judgement;

/// <summary>
/// Certain components need to be synchronized between the server and clients as a result of changes
/// For the components that do have the Synchronized + SynchronizedField attributes,
/// this generator will automatically create the functions necessary to network the marked fields
///
/// The created methods will encode the component's current fields to the BufferWriter
/// or update the component's fields using the BufferReader
/// 
/// also lmao, I wasted like 20 minutes debugging why it wasn't generating anything only to realize
/// I forgot to inherit IIncrementalGenerator
/// </summary>
[Generator]
public class ComponentSynchronizationGenerator : IIncrementalGenerator
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
                }).Where(typedSymbol => typedSymbol is not null);
        
        // I want an efficient way to refer to each component in a guaranteed order when writing packets
        // To do this, I need to gather all of our components into one context (the collect)
        // and then order them alphabetically into a string, so that I can use that string
        // I mean, at least I think it's alphabetical. Whatever the default sort method is, it doesn't really matter
        // as long as it's consistent.
        var alphabeticalComponentString = synchronizedComponents
            .Select(static (typedSymbol, _) => typedSymbol?.Name).Collect()
            .Select(static (typedSymbolName, _) => string.Join(",", typedSymbolName.Sort()));
        
        // Then we need to go one by one again through our component list,
        // but now with the full component name context in sorted order,
        // and then attach an index number that we're using as our ID to each component
        var componentAndIds = synchronizedComponents
            .Combine(alphabeticalComponentString)
            .Select(static (typedSymbolAndString, _) =>
        {
            var typedSymbol = typedSymbolAndString.Left;
            var alphabeticalString = typedSymbolAndString.Right;

            var sortedComponentNames = alphabeticalString.Split(',');
            var componentId = (ushort)Array.IndexOf(sortedComponentNames, typedSymbol?.Name);
            return (typedSymbol, componentId);
        });
        
        context.RegisterSourceOutput(componentAndIds, GenerateComponentSynchronizationFiles);
    }
    
    private void GenerateComponentSynchronizationFiles(SourceProductionContext context, (ITypeSymbol? typedSymbol, ushort componentId) component)
    {
        if (component.typedSymbol is null)
            return;
        
        var symbolNameSpace = component.typedSymbol.ContainingNamespace.ToDisplayString();
        var className = component.typedSymbol.Name;
        var properties = component.typedSymbol.GetMembers().ToList();
        Dictionary<IFieldSymbol, ImmutableArray<KeyValuePair<string,TypedConstant>>> unreliableFields = [];
        Dictionary<IFieldSymbol, ImmutableArray<KeyValuePair<string,TypedConstant>>> onChangeFields = [];
        foreach (var property in properties)
        {
            var propertyAttributes = property.GetAttributes().ToList();
            var synchronizedAttribute = propertyAttributes.Find(x => x.AttributeClass?.Name == "SynchronizedField");
            if (synchronizedAttribute == null)
                continue;

            if (property is not IFieldSymbol fieldSymbol)
                continue;
            
            var arguments = synchronizedAttribute.NamedArguments;
            var unreliable = true;
            foreach (var argument in arguments)
            {
                if (argument.Key != "OnChange")
                    continue;

                if (argument.Value.Value is not bool onChange)
                    continue;

                if (!onChange)
                    break;
                
                unreliable = false;
                break;
            }
            
            if (unreliable)
                unreliableFields.Add(fieldSymbol, arguments);
            else
                onChangeFields.Add(fieldSymbol, arguments);
        }
        // Get fieldSymbol, get type, truncate everything before the last . and add it to the using declarations
        
        var sourceBuilder = new StringBuilder(@"using System;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;
");
        sourceBuilder.AppendLine($@"
namespace {symbolNameSpace};

#nullable enable

// <<auto-generated by the ComponentSynchronizationGenerator.cs>>
public partial class {className} : ISynchronizedComponent
{{");
        sourceBuilder.AppendLine($"    public const ushort ComponentId = {component.componentId};");
        sourceBuilder.AppendLine();
        
        // WriteUnreliable
        sourceBuilder.AppendLine(
            "    // This is where fields whose values need to be constantly communicated to other users goes");
        sourceBuilder.AppendLine(@"    public void WriteUnreliableChannel(ref BufferWriter bufferWriter)
    {");
        foreach (var field in unreliableFields)
        {
            var writeString = BufferWriterWriter(field.Key, field.Value);
            if (writeString.Length <= 0)
                continue;
            
            sourceBuilder.AppendLine($"        bufferWriter.{writeString};");
        }
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        // ReadUnreliable
        sourceBuilder.AppendLine(@"    public void ReadUnreliableChannel(ref BufferReader bufferReader)
    {");
        foreach (var field in unreliableFields)
        {
            var writeString = BufferReaderWriter(field.Key, field.Value);
            if (writeString.Length <= 0)
                continue;
            
            sourceBuilder.AppendLine($"        {field.Key.Name} = bufferReader.{writeString};");
        }
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        // WriteReliable
        sourceBuilder.AppendLine(
            "    // This is where fields whose values only need to be communicated to users when they change goes");
        sourceBuilder.AppendLine(@"    public void WriteReliableChannel(ref BufferWriter bufferWriter)
    {");
        foreach (var field in onChangeFields)
        {
            var writeString = BufferWriterWriter(field.Key, field.Value);
            if (writeString.Length <= 0)
                continue;
            
            sourceBuilder.AppendLine($"        bufferWriter.{writeString};");
        }
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        
        // ReadReliable
        sourceBuilder.AppendLine(@"    public void ReadReliableChannel(ref BufferReader bufferReader)
    {");
        foreach (var field in onChangeFields)
        {
            var writeString = BufferReaderWriter(field.Key, field.Value);
            if (writeString.Length <= 0)
                continue;
            
            sourceBuilder.AppendLine($"        {field.Key.Name} = bufferReader.{writeString};");
        }
        sourceBuilder.AppendLine("    }");
        
        sourceBuilder.AppendLine(@"}");
        
        
        context.AddSource($"Synchronize.{className}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// Returns the correct Write[THING] method string for a given field
    /// </summary>
    /// <param name="fieldSymbol">Any kind of field with the SynchronizedField attribute</param>
    /// <param name="synchronizedFieldArguments">The named arguments on that SynchronizedField attribute</param>
    /// <returns></returns>
    private string BufferWriterWriter(IFieldSymbol fieldSymbol,
        ImmutableArray<KeyValuePair<string, TypedConstant>> synchronizedFieldArguments)
    {
        switch (fieldSymbol.Type.Name)
        {
            case "Boolean":
                return $"WriteBool({fieldSymbol.Name})";
            case "Int32":
                return $"WriteInt({fieldSymbol.Name}" + WriteIntMinMax(synchronizedFieldArguments) + ")";
            case "Single":
                return $"WriteFloat({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Vector2":
                return $"WriteVector2({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Vector2I":
                return $"WriteVector2I({fieldSymbol.Name}" + WriteIntMinMax(synchronizedFieldArguments) + ")";
            case "Rect2":
                return $"WriteRect2({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Rect2I":
                return $"WriteRect2I({fieldSymbol.Name}" + WriteIntMinMax(synchronizedFieldArguments) + ")";
            case "Vector3":
                return $"WriteVector3({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Vector3I":
                return $"WriteVector3I({fieldSymbol.Name}" + WriteIntMinMax(synchronizedFieldArguments) + ")";
            case "Basis":
                return $"WriteBasis({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Transform3D":
                return $"WriteTransform3D({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Vector4":
                return $"WriteVector4({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Vector4I":
                return $"WriteVector4I({fieldSymbol.Name}" + WriteIntMinMax(synchronizedFieldArguments) + ")";
            case "Plane":
                return $"WritePlane({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Aabb":
                return $"WriteAabb({fieldSymbol.Name}" + WriteFloatMinMaxPrecision(synchronizedFieldArguments) + ")";
            case "Quaternion":
                return $"WriteQuaternion({fieldSymbol.Name})";
        }

        return "";
    }
    
    /// <summary>
    /// Returns the correct Read[THING] method string for a given field
    /// </summary>
    /// <param name="fieldSymbol">Any kind of field with the SynchronizedField attribute</param>
    /// <param name="synchronizedFieldArguments">The named arguments on that SynchronizedField attribute</param>
    /// <returns></returns>
    private string BufferReaderWriter(IFieldSymbol fieldSymbol,
        ImmutableArray<KeyValuePair<string, TypedConstant>> synchronizedFieldArguments)
    {
        switch (fieldSymbol.Type.Name)
        {
            case "Boolean":
                return $"ReadBool()";
            case "Int32":
                return $"ReadInt(" + WriteIntMinMax(synchronizedFieldArguments).Substring(2) + ")";
            case "Single":
                return $"ReadFloat(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector2":
                return $"ReadVector2(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector2I":
                return $"ReadVector2I(" + WriteIntMinMax(synchronizedFieldArguments).Substring(2) + ")";
            case "Rect2":
                return $"ReadRect2(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Rect2I":
                return $"ReadRect2I(" + WriteIntMinMax(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector3":
                return $"ReadVector3(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector3I":
                return $"ReadVector3I(" + WriteIntMinMax(synchronizedFieldArguments).Substring(2) + ")";
            case "Basis":
                return $"ReadBasis(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Transform3D":
                return $"ReadTransform3D(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector4":
                return $"ReadVector4(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Vector4I":
                return $"ReadVector4I(" + WriteIntMinMax(synchronizedFieldArguments).Substring(2) + ")";
            case "Plane":
                return $"ReadPlane(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Aabb":
                return $"ReadAabb(" + WriteFloatMinMaxPrecision(synchronizedFieldArguments).Substring(2) + ")";
            case "Quaternion":
                return $"ReadQuaternion()";
        }

        return "";
    }

    private string WriteIntMinMax(ImmutableArray<KeyValuePair<string, TypedConstant>> fieldArguments)
    {
        var result = "";
        
        foreach (var argument in fieldArguments)
        {
            if (argument.Key == "Min" && argument.Value.Value is float minimum)
                result += $", (int){minimum}";
                
            if (argument.Key == "Max" && argument.Value.Value is float maximum)
                result += $", (int){maximum}";
        }
        
        return result;
    }

    private string WriteFloatMinMaxPrecision(ImmutableArray<KeyValuePair<string, TypedConstant>> fieldArguments)
    {
        var result = "";
        
        foreach (var argument in fieldArguments)
        {
            if (argument.Key == "Min" && argument.Value.Value is float minimum)
                result += $", {minimum}f";
                    
            if (argument.Key == "Max" && argument.Value.Value is float maximum)
                result += $", {maximum}f";
                    
            if (argument.Key == "Precision" &&  argument.Value.Value is float precision)
                result += $", {precision}f";
        }
        
        return result;
    }
}