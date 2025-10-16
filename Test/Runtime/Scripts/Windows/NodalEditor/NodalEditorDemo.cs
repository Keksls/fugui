using Fu;
using Fu.Framework;
using Fu.Framework.Nodal;
using UnityEngine;

public class NodalEditorDemo : FuWindowBehaviour
{
    [SerializeField] Color _floatColor;
    [SerializeField] Color _intColor;
    [SerializeField] Color _vector2Color;
    [SerializeField] Color _vector3Color;
    [SerializeField] Color _vector4Color;
    [SerializeField] Color _field2DColor;
    private FuNodalEditor _nodalEditor;

    private void Awake()
    {
        _nodalEditor = new FuNodalEditor("Demo Nodal Editor");

        // TYPES (choose your palette colors)
        FuNodalRegistry.RegisterType(FuNodalType.Create<float>("core/float", 0f, o => o.ToString(), s => float.Parse(s), color: _floatColor));
        FuNodalRegistry.RegisterType(FuNodalType.Create<int>("core/int", 0, o => o.ToString(), s => int.Parse(s), color: _intColor));
        FuNodalRegistry.RegisterType(FuNodalType.Create<UnityEngine.Vector2>("core/v2", UnityEngine.Vector2.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<UnityEngine.Vector2>(s), color: _vector2Color));
        FuNodalRegistry.RegisterType(FuNodalType.Create<UnityEngine.Vector3>("core/v3", UnityEngine.Vector3.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<UnityEngine.Vector3>(s), color: _vector3Color));
        FuNodalRegistry.RegisterType(FuNodalType.Create<UnityEngine.Vector4>("core/v4", UnityEngine.Vector4.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<UnityEngine.Vector4>(s), color: _vector4Color));
        FuNodalRegistry.RegisterType(
            FuNodalType.Create<Fu.Framework.Procedural.IField2D>(
                "core/field2D",
                new Fu.Framework.Procedural.Fields.ConstantField2D(0f),
                obj => "\"field2d\"",
                str => new Fu.Framework.Procedural.Fields.ConstantField2D(0f),
                color: _field2DColor
            )
        );

        // VARIABLES (same color family as your Variables palette)
        FuNodalRegistry.RegisterNode("Variables/Float", () => new Fu.Framework.FloatNode(_floatColor));
        FuNodalRegistry.RegisterNode("Variables/Int", () => new Fu.Framework.IntNode(_intColor));
        FuNodalRegistry.RegisterNode("Variables/Vector2", () => new Fu.Framework.Vector2Node(_vector2Color));
        FuNodalRegistry.RegisterNode("Variables/Vector3", () => new Fu.Framework.Vector3Node(_vector3Color));
        FuNodalRegistry.RegisterNode("Variables/Vector4", () => new Fu.Framework.Vector4Node(_vector4Color));

        // GENERATORS
        FuNodalRegistry.RegisterNode("Procedural/Float → Field", () => new Fu.Framework.FloatToFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Noise Field", () => new Fu.Framework.NoiseFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/FBM Field", () => new Fu.Framework.FBMFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Ridge Field", () => new Fu.Framework.RidgeFieldNode());

        // COMBINERS & MATH
        FuNodalRegistry.RegisterNode("Procedural/Add", () => new Fu.Framework.AddFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Multiply", () => new Fu.Framework.MulFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Min", () => new Fu.Framework.MinFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Max", () => new Fu.Framework.MaxFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Lerp (A,B,T)", () => new Fu.Framework.LerpFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Clamp", () => new Fu.Framework.ClampFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Saturate", () => new Fu.Framework.SaturateFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Remap", () => new Fu.Framework.RemapFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Abs", () => new Fu.Framework.AbsFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Invert01", () => new Fu.Framework.Invert01FieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Pow", () => new Fu.Framework.PowFieldNode());

        // TRANSFORMS & ANALYSIS
        FuNodalRegistry.RegisterNode("Procedural/Transform (T/S/R)", () => new Fu.Framework.TransformFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Warp (U,V,Strength)", () => new Fu.Framework.WarpFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Slope", () => new Fu.Framework.SlopeFieldNode());
        FuNodalRegistry.RegisterNode("Procedural/Erosion (Approx)", () => new Fu.Framework.ErosionNode());

        // UTILITIES
        FuNodalRegistry.RegisterNode("Procedural/Sample Field", () => new Fu.Framework.SampleFieldNode());

    }

    public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
    {
        base.OnWindowDefinitionCreated(windowDefinition);
        FuOverlay overlay = new FuOverlay("editorMinimap", new Vector2Int(128, 128),
            (overlay, layout) =>
            {
                _nodalEditor.DrawMiniMap(new Vector2(128, 128));
            });
        overlay.AnchorWindowDefinition(windowDefinition, FuOverlayAnchorLocation.BottomLeft);
    }

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        base.OnUI(window, layout);
        _nodalEditor.Draw(window);
    }
}