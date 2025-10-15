using Fu;
using Fu.Framework;
using Fu.Framework.Nodal;
using UnityEngine;

public class NodalEditorDemo : FuWindowBehaviour
{
    [SerializeField] Color _floatColor;
    [SerializeField] Color _vector2Color;
    [SerializeField] Color _vector3Color;
    private FuNodalEditor _nodalEditor;

    private void Awake()
    {
        _nodalEditor = new FuNodalEditor("Demo Nodal Editor");

        _nodalEditor.RegisterNode("Variables/Float", () => new FloatNode());
        _nodalEditor.RegisterNode("Variables/Int", () => new IntNode());
        _nodalEditor.RegisterNode("Variables/Vector2", () => new Vector2Node());
        _nodalEditor.RegisterNode("Variables/Vector3", () => new Vector3Node());
        _nodalEditor.RegisterNode("Variables/Vector4", () => new Vector4Node());

        _nodalEditor.RegisterNode("Maths/Add", () => new AddNode());
        _nodalEditor.RegisterNode("Maths/Subtract", () => new SubtractNode());
        _nodalEditor.RegisterNode("Maths/Multiply", () => new MultiplyNode());
        _nodalEditor.RegisterNode("Maths/Divide", () => new DivideNode());
        _nodalEditor.RegisterNode("Maths/Min", () => new MinNode());
        _nodalEditor.RegisterNode("Maths/Max", () => new MaxNode());
        _nodalEditor.RegisterNode("Maths/Pow", () => new PowNode());
        _nodalEditor.RegisterNode("Maths/Clamp", () => new ClampNode());
        _nodalEditor.RegisterNode("Maths/Lerp", () => new LerpNode());

        _nodalEditor.RegisterNode("Maths/Saturate", () => new SaturateNode());
        _nodalEditor.RegisterNode("Maths/OneMinus", () => new OneMinusNode());
        _nodalEditor.RegisterNode("Maths/Abs", () => new AbsNode());
        _nodalEditor.RegisterNode("Maths/Floor", () => new FloorNode());
        _nodalEditor.RegisterNode("Maths/Ceil", () => new CeilNode());
        _nodalEditor.RegisterNode("Maths/Round", () => new RoundNode());
        _nodalEditor.RegisterNode("Maths/Sqrt", () => new SqrtNode());

        _nodalEditor.RegisterNode("Maths/Dot", () => new DotNode());
        _nodalEditor.RegisterNode("Maths/Cross", () => new CrossNode());
        _nodalEditor.RegisterNode("Maths/Length", () => new LengthNode());
        _nodalEditor.RegisterNode("Maths/Normalize", () => new NormalizeNode());
        _nodalEditor.RegisterNode("Maths/Distance", () => new DistanceNode());
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