using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Multiply two float
    /// </summary>
    public sealed class MultNode : FuNode
    {
        public override string Title => "Multiply";
        public override float Width => 128f;

        /// <summary>
        /// Creates the default ports for this node.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            AddPort(new FuNodalPort { Name = "A", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "B", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 1f, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = new HashSet<string> { "core/float" }, Data = 0f, Multiplicity = FuNodalMultiplicity.Many });
        }

        /// <summary>
        /// Checks if two ports can be connected.
        /// </summary>
        /// <param name="fromPort"> the output port from another node.</param>
        /// <param name="toPort"> the input port of this node.</param>
        /// <returns> true if they can be connected, false otherwise.</returns>
        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort)
        {
            switch (fromPort.DataType)
            {
                case "core/float":
                case "core/int":
                case "core/v2":
                case "core/v3":
                case "core/v4":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Computes the output value based on the input values.
        /// </summary>
        public override void Compute()
        {
            string dataTypeA = GetPortType("A");
            string dataTypeB = GetPortType("B");

            // Decide output type (largest vector wins; otherwise float, unless both int)
            string outputType = "core/float";
            if (dataTypeA == dataTypeB && dataTypeA == "core/int") outputType = "core/int";
            else if (dataTypeA == dataTypeB && dataTypeA == "core/float") outputType = "core/float";
            else if (dataTypeA == "core/v4" || dataTypeB == "core/v4") outputType = "core/v4";
            else if (dataTypeA == "core/v3" || dataTypeB == "core/v3") outputType = "core/v3";
            else if (dataTypeA == "core/v2" || dataTypeB == "core/v2") outputType = "core/v2";

            // Helpers
            int Dim(string t) => t == "core/v4" ? 4 : t == "core/v3" ? 3 : t == "core/v2" ? 2 : 1;

            float GetScalar(string port, string t)
            {
                if (t == "core/int") return (float)GetPortValue<int>(port, 1);
                if (t == "core/float") return GetPortValue<float>(port, 1f);
                if (t == "core/v2") { var v = GetPortValue<Vector2>(port, Vector2.zero); return v.x; }
                if (t == "core/v3") { var v = GetPortValue<Vector3>(port, Vector3.zero); return v.x; }
                if (t == "core/v4") { var v = GetPortValue<Vector4>(port, Vector4.zero); return v.x; }
                return 1f;
            }

            Vector4 GetVec4(string port, string t)
            {
                if (t == "core/int") { float s = GetScalar(port, t); return new Vector4(s, s, s, s); }
                if (t == "core/float") { float s = GetScalar(port, t); return new Vector4(s, s, s, s); }
                if (t == "core/v2") { var v = GetPortValue<Vector2>(port, Vector2.zero); return new Vector4(v.x, v.y, 0f, 0f); }
                if (t == "core/v3") { var v = GetPortValue<Vector3>(port, Vector3.zero); return new Vector4(v.x, v.y, v.z, 0f); }
                if (t == "core/v4") { return GetPortValue<Vector4>(port, Vector4.zero); }
                return Vector4.zero;
            }

            // Read inputs
            int dimA = Dim(dataTypeA);
            int dimB = Dim(dataTypeB);

            // Scalar × Scalar
            if (dimA == 1 && dimB == 1 && (outputType == "core/float" || outputType == "core/int"))
            {
                // If either is float → float
                if (outputType == "core/float")
                {
                    float a = GetScalar("A", dataTypeA);
                    float b = GetScalar("B", dataTypeB);
                    SetPortValue("Out", "core/float", a * b);
                }
                else // both int
                {
                    int a = GetPortValue<int>("A", 1);
                    int b = GetPortValue<int>("B", 1);
                    SetPortValue("Out", "core/int", a * b);
                }
                return;
            }

            // Vector/scalar or vector/vector
            Vector4 va = GetVec4("A", dataTypeA);
            Vector4 vb = GetVec4("B", dataTypeB);

            int outDim = Dim(outputType);
            int minDim = Mathf.Min(dimA, dimB);
            if (minDim == 1)
                minDim = Mathf.Max(dimA, dimB);

            // Multiply component-wise up to smallest dimension.
            // For remaining components (if output is larger), keep the component from the larger-typed input.
            Vector4 r = Vector4.zero;
            for (int i = 0; i < minDim; i++)
            {
                r[i] = va[i] * vb[i];
            }
            for (int i = minDim; i < outDim; i++)
            {
                // If A has that component, keep A's; else keep B's; else 0
                float keep = (i < dimA) ? va[i] : (i < dimB) ? vb[i] : 0f;
                r[i] = keep;
            }

            // Write as the decided output type
            switch (outputType)
            {
                case "core/v2":
                    SetPortValue("Out", "core/v2", new Vector2(r.x, r.y));
                    break;
                case "core/v3":
                    SetPortValue("Out", "core/v3", new Vector3(r.x, r.y, r.z));
                    break;
                case "core/v4":
                    SetPortValue("Out", "core/v4", new Vector4(r.x, r.y, r.z, r.w));
                    break;
                default: // "core/float"
                         // If a vector sneaked in but outputType fell back to float, take x
                    SetPortValue("Out", "core/float", r.x);
                    break;
            }
        }

        /// <summary>
        /// Draws the node GUI.
        /// </summary>
        /// <param name="layout"> The layout to draw on.</param>
        public override void OnDraw(FuLayout layout)
        {
            string dataType = GetPortType("Out");
            layout.DisableNextElement();

            switch (dataType)
            {
                case "core/float":
                case "core/int":
                    float valueFloat = GetPortValue<float>("Out", 0f);
                    layout.Drag("##" + Id, ref valueFloat, "", float.MinValue, float.MaxValue);
                    break;

                case "core/v2":
                    Vector2 valueV2 = GetPortValue<Vector2>("Out", Vector2.zero);
                    layout.Drag("##" + Id, ref valueV2, "");
                    break;

                case "core/v3":
                    Vector3 valueV3 = GetPortValue<Vector3>("Out", Vector3.zero);
                    layout.Drag("##" + Id, ref valueV3, "");
                    break;

                case "core/v4":
                    Vector4 valueV4 = GetPortValue<Vector4>("Out", Vector4.zero);
                    layout.Drag("##" + Id, ref valueV4, "");
                    break;
            }
        }

        /// <summary>
        /// Sets the default values for the given port.
        /// </summary>
        /// <param name="port"> The port to set default values for.</param>
        public override void SetDefaultValues(FuNodalPort port)
        {
            switch (port.DataType)
            {
                case "core/float":
                    port.Data = 0f;
                    break;

                case "core/int":
                    port.Data = 0;
                    break;

                case "core/v2":
                    port.Data = Vector2.zero;
                    break;

                case "core/v3":
                    port.Data = Vector3.zero;
                    break;

                case "core/v4":
                    port.Data = Vector4.zero;
                    break;
            }
        }
    }
}