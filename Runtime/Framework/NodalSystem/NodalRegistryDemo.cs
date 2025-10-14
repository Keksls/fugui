using Fu.Framework.Nodal;
using System;
using UnityEngine;

public class NodalRegistryDemo : MonoBehaviour
{
    [SerializeField] Color _multiplyColor;
    [SerializeField] Color _addColor;
    [SerializeField] Color _floatColor;

    private void Awake()
    {
        // Register custom types
        NodalTypeRegistry.Register(new FloatType());
        NodalTypeRegistry.Register(new Vector2Type());

        // Multiply
        NodalNodeRegistry.Register(
            NodeBuilder.Define("math/multiply", "Multiply")
                .Width(220f)
                .In("A", "float", NodalMultiplicity.Single)
                .In("B", "float", NodalMultiplicity.Single)
                .Out("Out", "float", NodalMultiplicity.Many)
                .UI(20f, (node, layout) =>
                {
                    float val = node.GetPortvalue("Out", 1.0f);
                    layout.Text($"Result: {val}");
                }).Compute((ctx, node) =>
                {
                    float a = ctx.GetInput(node, "A", 1.0f);
                    float b = ctx.GetInput(node, "B", 1.0f);
                    ctx.SetOutput(node, "Out", a + b);
                })
                .Color(_multiplyColor)
                .Build()
        );

        // Multiply
        NodalNodeRegistry.Register(
            NodeBuilder.Define("math/add", "Add")
                .Width(220f)
                .In("A", "float", NodalMultiplicity.Single)
                .In("B", "float", NodalMultiplicity.Single)
                .Out("Out", "float", NodalMultiplicity.Many)
                .UI(20f, (node, layout) =>
                {
                    float val = node.GetPortvalue("Out", 1.0f);
                    layout.Text($"Result: {val}");
                }).Compute((ctx, node) =>
                {
                    float a = ctx.GetInput(node, "A", 1.0f);
                    float b = ctx.GetInput(node, "B", 1.0f);
                    ctx.SetOutput(node, "Out", a + b);
                })
                .Color(_addColor)
                .Build()
        );

        // Float value
        NodalNodeRegistry.Register(
            NodeBuilder.Define("variables/float", "Float Value")
                .Width(160f)
                .Out("Value", "float", NodalMultiplicity.Many)
                .UI(24f, (node, layout) =>
                {
                    float current = node.GetPortvalue("Value", 1.0f);
                    if (layout.Drag("##" + node.Id + "value", ref current, "", float.MinValue, float.MaxValue))
                        node.SetPortValue("Value", current);
                })
                .Color(_floatColor)
                .Build()
        );
    }
}

public sealed class FloatType : INodalType
{
    public string TypeId => "float";
    public Type ClrType => typeof(float);
    public string Serialize(object value) =>
        ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
    public object Deserialize(string data) =>
        float.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
}

public sealed class Vector2Type : INodalType
{
    public string TypeId => "vec2";
    public Type ClrType => typeof(UnityEngine.Vector2);
    public string Serialize(object value) =>
        JsonUtility.ToJson((Vector2)value);
    public object Deserialize(string data) =>
        JsonUtility.FromJson<Vector2>(data);
}