using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework.Demo
{
    /// <summary>
    /// Base class for binary arithmetic nodes (Add, Sub, Mul, Div).
    /// Handles common logic for port creation, type conversion, vector handling, and drawing.
    /// </summary>
    public abstract class BinaryOperationNode : FuNode
    {
        public override float Width => 128f;

        /// <summary>
        /// Operation delegate applied to two float values.
        /// </summary>
        protected abstract float Operate(float a, float b);

        /// <summary>
        /// The default value used for initialization (e.g., 0 for Add/Sub, 1 for Mul/Div).
        /// </summary>
        protected abstract float DefaultValue { get; }

        /// <summary>
        /// Creates the default ports for this node.
        /// </summary>
        public override void CreateDefaultPorts()
        {
            var allowed = new HashSet<string> { "core/float", "core/int", "core/v2", "core/v3", "core/v4" };
            AddPort(new FuNodalPort { Name = "A", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = allowed, Data = DefaultValue, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "B", Direction = FuNodalPortDirection.In, DataType = "core/float", AllowedTypes = allowed, Data = DefaultValue, Multiplicity = FuNodalMultiplicity.Single });
            AddPort(new FuNodalPort { Name = "Out", Direction = FuNodalPortDirection.Out, DataType = "core/float", AllowedTypes = allowed, Data = DefaultValue, Multiplicity = FuNodalMultiplicity.Many });
        }

        /// <summary>
        /// Computes the output value based on the input values.
        /// </summary>
        public override void Compute()
        {
            string dataTypeA = GetPortType("A");
            string dataTypeB = GetPortType("B");

            string outputType = ResolveOutputType(dataTypeA, dataTypeB);
            int dimA = GetDimension(dataTypeA);
            int dimB = GetDimension(dataTypeB);

            if (dimA == 1 && dimB == 1 && (outputType == "core/float" || outputType == "core/int"))
            {
                if (outputType == "core/float")
                {
                    float a = GetScalar("A", dataTypeA);
                    float b = GetScalar("B", dataTypeB);
                    SetPortValue("Out", "core/float", Operate(a, b));
                }
                else
                {
                    int a = GetPortValue<int>("A", (int)DefaultValue);
                    int b = GetPortValue<int>("B", (int)DefaultValue);
                    SetPortValue("Out", "core/int", (int)Operate(a, b));
                }
                return;
            }

            Vector4 va = GetVec4("A", dataTypeA);
            Vector4 vb = GetVec4("B", dataTypeB);

            int outDim = GetDimension(outputType);
            int minDim = Mathf.Min(dimA, dimB);
            if (minDim == 1)
                minDim = Mathf.Max(dimA, dimB);

            Vector4 r = Vector4.zero;
            for (int i = 0; i < minDim; i++)
                r[i] = Operate(va[i], vb[i]);

            for (int i = minDim; i < outDim; i++)
                r[i] = (i < dimA) ? va[i] : (i < dimB) ? vb[i] : 0f;

            switch (outputType)
            {
                case "core/v2": SetPortValue("Out", "core/v2", new Vector2(r.x, r.y)); break;
                case "core/v3": SetPortValue("Out", "core/v3", new Vector3(r.x, r.y, r.z)); break;
                case "core/v4": SetPortValue("Out", "core/v4", new Vector4(r.x, r.y, r.z, r.w)); break;
                default: SetPortValue("Out", "core/float", r.x); break;
            }
        }

        /// <summary>
        /// Gets the highest-level compatible output type between two ports.
        /// </summary>
        private string ResolveOutputType(string a, string b)
        {
            if (a == b && a == "core/int") return "core/int";
            if (a == b && a == "core/float") return "core/float";
            if (a == "core/v4" || b == "core/v4") return "core/v4";
            if (a == "core/v3" || b == "core/v3") return "core/v3";
            if (a == "core/v2" || b == "core/v2") return "core/v2";
            return "core/float";
        }

        private int GetDimension(string t)
        {
            switch (t)
            {
                case "core/v4": return 4;
                case "core/v3": return 3;
                case "core/v2": return 2;
                default: return 1;
            }
        }

        private float GetScalar(string port, string t)
        {
            if (t == "core/int") return (float)GetPortValue<int>(port, (int)DefaultValue);
            if (t == "core/float") return GetPortValue<float>(port, DefaultValue);
            if (t == "core/v2") { var v = GetPortValue<Vector2>(port, Vector2.zero); return v.x; }
            if (t == "core/v3") { var v = GetPortValue<Vector3>(port, Vector3.zero); return v.x; }
            if (t == "core/v4") { var v = GetPortValue<Vector4>(port, Vector4.zero); return v.x; }
            return DefaultValue;
        }

        private Vector4 GetVec4(string port, string t)
        {
            if (t == "core/int" || t == "core/float")
            {
                float s = GetScalar(port, t);
                return new Vector4(s, s, s, s);
            }
            if (t == "core/v2")
            {
                var v = GetPortValue<Vector2>(port, Vector2.zero);
                return new Vector4(v.x, v.y, 0f, 0f);
            }
            if (t == "core/v3")
            {
                var v = GetPortValue<Vector3>(port, Vector3.zero);
                return new Vector4(v.x, v.y, v.z, 0f);
            }
            if (t == "core/v4")
                return GetPortValue<Vector4>(port, Vector4.zero);
            return Vector4.zero;
        }

        public override void OnDraw(FuLayout layout)
        {
            string dataType = GetPortType("Out");
            layout.DisableNextElement();

            switch (dataType)
            {
                case "core/float":
                case "core/int":
                    float valueFloat = GetPortValue<float>("Out", DefaultValue);
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

        public override void SetDefaultValues(FuNodalPort port)
        {
            port.Data = 1f;
            port.DataType = "core/float";
        }

        public override string GetCurrentConvertedType(FuNodalPort port)
        {
            if (port.Name != "A" && port.Name != "B")
                return port.DataType;

            string thisPort = GetPortType(port.Name);
            string otherPort = GetPortType(port.Name == "A" ? "B" : "A");
            return ResolveOutputType(thisPort, otherPort);
        }
    }
}