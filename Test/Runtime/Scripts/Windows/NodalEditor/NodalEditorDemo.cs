using Fu.Framework.Nodal;
using System;
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
        private FuNodalGraph _nodalgraph;
        private FuNodalEditor _nodalEditor;

        private void Awake()
        {
            _nodalgraph = new FuNodalGraph { Name = "Demo Graph" };
            _nodalEditor = new FuNodalEditor(_nodalgraph);
            _nodalEditor.UseBezierCurves = false;

            // TYPES (choose your palette colors)
            _nodalgraph.Registry.RegisterType(FuNodalType.Create<float>("core/float", 0f, o => o.ToString(), s => float.Parse(s), color: _floatColor));
            _nodalgraph.Registry.RegisterType(FuNodalType.Create<int>("core/int", 0, o => o.ToString(), s => int.Parse(s), color: _intColor));
            _nodalgraph.Registry.RegisterType(FuNodalType.Create<Vector2>("core/v2", Vector2.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector2>(s), color: _vector2Color));
            _nodalgraph.Registry.RegisterType(FuNodalType.Create<Vector3>("core/v3", Vector3.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector3>(s), color: _vector3Color));
            _nodalgraph.Registry.RegisterType(FuNodalType.Create<Vector4>("core/v4", Vector4.zero, o => JsonUtility.ToJson(o), s => JsonUtility.FromJson<Vector4>(s), color: _vector4Color));

            // VARIABLES (same color family as your Variables palette)
            _nodalgraph.Registry.RegisterNode("Variables/Float", () => new FloatNode(_floatColor));
            _nodalgraph.Registry.RegisterNode("Variables/Int", () => new IntNode(_intColor));
            _nodalgraph.Registry.RegisterNode("Variables/Vector2", () => new Vector2Node(_vector2Color));
            _nodalgraph.Registry.RegisterNode("Variables/Vector3", () => new Vector3Node(_vector3Color));
            _nodalgraph.Registry.RegisterNode("Variables/Vector4", () => new Vector4Node(_vector4Color));

            // COMBINERS & MATH
            _nodalgraph.Registry.RegisterNode("Maths/Add", () => new AddNode());
            _nodalgraph.Registry.RegisterNode("Maths/Sub", () => new SubNode());
            _nodalgraph.Registry.RegisterNode("Maths/Mult", () => new MultNode());
            _nodalgraph.Registry.RegisterNode("Maths/Div", () => new DivNode());

            // Add new / save / load graph to context menu
            _nodalEditor.RegisterCustomContextMenu((builder) =>
            {
                builder.AddSeparator()
                    .AddItem("New", () => { _nodalEditor.Graph = new FuNodalGraph { Name = "New Graph" }; })
                    .AddItem("Save JSON", () =>
                    {
                        string json = FuGraphSerializer.ToJson(_nodalEditor.Graph);
                        string path = FileBrowser.SaveFilePanel("Save Nodal Graph JSON", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _nodalEditor.Graph.Name + ".json", "json");
                        if (string.IsNullOrEmpty(path)) return;
                        System.IO.File.WriteAllText(path, json);
                    })
                    .AddItem("Load JSON", () =>
                    {
                        string[] path = FileBrowser.OpenFilePanel("Load Nodal Graph JSON", Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "json", false);
                        if (path.Length == 0) return;
                        string json = System.IO.File.ReadAllText(path[0]);
                        _nodalEditor.Graph.FromJson(json);
                    });
            });
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