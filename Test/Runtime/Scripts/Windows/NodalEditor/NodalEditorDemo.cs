using Fu;
using Fu.Framework;
using Fu.Framework.Nodal;
using UnityEngine;

namespace Fu.Framework.Demo
{
    public class NodalEditorDemo : FuWindowBehaviour
    {
        [SerializeField] Color _floatColor;
        [SerializeField] Color _intColor;
        [SerializeField] Color _vector2Color;
        [SerializeField] Color _vector3Color;
        [SerializeField] Color _vector4Color;
        private FuNodalEditor _nodalEditor;

        private void Awake()
        {
            _nodalEditor = new FuNodalEditor("Demo Nodal Editor");

            // TYPES (choose your palette colors)
            FuNodalRegistry.RegisterType(FuNodalType.Create<float>("core/float", 0f, o => o.ToString(), s => float.Parse(s), color: _floatColor));
            FuNodalRegistry.RegisterType(FuNodalType.Create<int>("core/int", 0, o => o.ToString(), s => int.Parse(s), color: _intColor));
            FuNodalRegistry.RegisterType(FuNodalType.Create<Vector2>("core/v2", Vector2.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector2>(s), color: _vector2Color));
            FuNodalRegistry.RegisterType(FuNodalType.Create<Vector3>("core/v3", Vector3.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector3>(s), color: _vector3Color));
            FuNodalRegistry.RegisterType(FuNodalType.Create<Vector4>("core/v4", Vector4.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector4>(s), color: _vector4Color));

            // VARIABLES (same color family as your Variables palette)
            FuNodalRegistry.RegisterNode("Variables/Float", () => new FloatNode(_floatColor));
            FuNodalRegistry.RegisterNode("Variables/Int", () => new IntNode(_intColor));
            FuNodalRegistry.RegisterNode("Variables/Vector2", () => new Vector2Node(_vector2Color));
            FuNodalRegistry.RegisterNode("Variables/Vector3", () => new Vector3Node(_vector3Color));
            FuNodalRegistry.RegisterNode("Variables/Vector4", () => new Vector4Node(_vector4Color));

            // COMBINERS & MATH
            FuNodalRegistry.RegisterNode("Procedural/Multiply", () => new MultNode());
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
}