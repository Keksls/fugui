using Fu;
using Fu.Framework;
using Fu.Framework.Nodal;
using UnityEngine;

public class NodalEditorDemo : FuWindowBehaviour
{
    [SerializeField] Color _floatColor;
    [SerializeField] Color _intColor;
    [SerializeField] Color _vector4Color;
    private FuNodalEditor _nodalEditor;

    private void Awake()
    {
        _nodalEditor = new FuNodalEditor("Demo Nodal Editor");

        // Register custom types
        FuNodalRegistry.RegisterType(FuNodalType.Create<float>("core/float", 0f, (obj) => obj.ToString(), (str) => float.Parse(str), color:_floatColor));
        FuNodalRegistry.RegisterType(FuNodalType.Create<int>("core/int", 0, (obj) => obj.ToString(), (str) => int.Parse(str), color: _intColor));
        FuNodalRegistry.RegisterType(FuNodalType.Create<Vector4>("core/v4", Vector4.zero, (obj) => string.Join('|', obj), (str) =>
        {
            string[] parts = str.Split('|');
            if (parts.Length != 4) return Vector4.zero;
            return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        }, color: _vector4Color));

        // Register custom nodes
        FuNodalRegistry.RegisterNode("Variables/Float", () => new FloatNode(_floatColor));
        FuNodalRegistry.RegisterNode("Variables/Int", () => new IntNode(_intColor));
        FuNodalRegistry.RegisterNode("Variables/Vector4", () => new Vector4Node(_vector4Color));
        FuNodalRegistry.RegisterNode("Maths/Add", () => new AddNode());
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