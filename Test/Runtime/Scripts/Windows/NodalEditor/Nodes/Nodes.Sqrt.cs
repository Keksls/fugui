using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// SqrtNode for the Fu nodal system. Generated automatically.
    /// </summary>
    public sealed class SqrtNode : FuNode
    {
        public override string Title => "Sqrt";
        public override float Width => 200f;
        public override Color? NodeColor => null;

        #region Helpers
        private static int SizeOf(string type)
        {
            switch (type)
            {
                case "core/float": return 1;
                case "core/int": return 1;
                case "core/v2": return 2;
                case "core/v3": return 3;
                case "core/v4": return 4;
                default: return 1;
            }
        }

        private static string OutVectorType(int size)
        {
            switch (size)
            {
                case 1: return "core/float";
                case 2: return "core/v2";
                case 3: return "core/v3";
                case 4: return "core/v4";
                default: return "core/float";
            }
        }

        private static float[] ReadToArray(FuNode node, string portName, string type, float defaultScalar = 0f)
        {
            int size = SizeOf(type);
            float[] arr = new float[Math.Max(1, size)];
            if (type == "core/int")
            {
                int v = node.GetPortValue<int>(portName, 0);
                arr[0] = v;
            }
            else if (type == "core/float")
            {
                arr[0] = node.GetPortValue<float>(portName, defaultScalar);
            }
            else if (type == "core/v2")
            {
                Vector2 v = node.GetPortValue<Vector2>(portName, Vector2.zero);
                arr[0] = v.x; arr[1] = v.y;
            }
            else if (type == "core/v3")
            {
                Vector3 v = node.GetPortValue<Vector3>(portName, Vector3.zero);
                arr[0] = v.x; arr[1] = v.y; arr[2] = v.z;
            }
            else if (type == "core/v4")
            {
                Vector4 v = node.GetPortValue<Vector4>(portName, Vector4.zero);
                arr[0] = v.x; arr[1] = v.y; arr[2] = v.z; arr[3] = v.w;
            }
            else
            {
                arr[0] = 0f;
            }
            return arr;
        }

        private static float[] Broadcast(float[] a, float[] b, int outSize, bool passThroughBForExcess)
        {
            // Broadcast arrays to outSize. For indices >= aLen or bLen:
            // - If passThroughBForExcess is True: result takes 'b' component for non-overlap (for binary ops like A op B).
            // - Otherwise, repeat last available value from each array.
            float[] ao = new float[outSize];
            float[] bo = new float[outSize];
            for (int i = 0; i < outSize; i++)
            {
                ao[i] = i < a.Length ? a[i] : (a.Length > 0 ? a[min(a.Length-1, 0)] : 0f);
                if (passThroughBForExcess)
                {
                    bo[i] = i < b.Length ? b[i] : (i < b.Length ? b[i] : (b.Length > 0 ? b[min(b.Length-1, 0)] : 0f));
                }
                else
                {
                    bo[i] = i < b.Length ? b[i] : (b.Length > 0 ? b[min(b.Length-1, 0)] : 0f);
                }
            }
            return new float[] {}; // dummy, not used directly
        }

        private static int min(int a, int b) => a < b ? a : b;

        private static void WriteFromArray(FuNode node, string outPort, string outType, float[] values)
        {
            switch (outType)
            {
                case "core/float":
                    node.SetPortValue(outPort, "core/float", values[0]);
                    break;
                case "core/v2":
                    node.SetPortValue(outPort, "core/v2", new Vector2(values[0], values[1]));
                    break;
                case "core/v3":
                    node.SetPortValue(outPort, "core/v3", new Vector3(values[0], values[1], values[2]));
                    break;
                case "core/v4":
                    node.SetPortValue(outPort, "core/v4", new Vector4(values[0], values[1], values[2], values[3]));
                    break;
                default:
                    node.SetPortValue(outPort, "core/float", values[0]);
                    break;
            }
        }

        private static float[] ExpandTo(float[] src, int target, float fillWith)
        {
            float[] r = new float[target];
            for (int i = 0; i < target; i++)
                r[i] = i < src.Length ? src[i] : fillWith;
            return r;
        }

        private static string ResolveOutType(string aType, string bType)
        {
            int sA = SizeOf(aType);
            int sB = SizeOf(bType);
            return OutVectorType(Math.Max(sA, sB));
        }
        #endregion

        public override bool CanConnect(FuNodalPort fromPort, FuNodalPort toPort) => true;

        public override void Compute()
        {
            string aType = GetPortType("A");
            int outSize = SizeOf(aType);
            string outType = OutVectorType(outSize);
            float[] a = ExpandTo(ReadToArray(this, "A", aType, 0f), outSize, 0f);
            float[] r = new float[outSize];
            for (int i = 0; i < outSize; i++)
                r[i] = Mathf.Sqrt(Mathf.Max(0f, a[i]));
            WriteFromArray(this, "Out", outType, r);
        }

        public override void CreateDefaultPorts()
        {
            FuNodalPort portA = new FuNodalPort
            {
                Name = "A",
                Direction = FuNodalPortDirection.In,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float", "core/int", "core/v2", "core/v3", "core/v4" },
                Data = 0f,
                Multiplicity = FuNodalMultiplicity.Single
            };
            AddPort(portA);

            FuNodalPort portOut = new FuNodalPort
            {
                Name = "Out",
                Direction = FuNodalPortDirection.Out,
                DataType = "core/float",
                AllowedTypes = new HashSet<string> { "core/float", "core/v2", "core/v3", "core/v4" },
                Data = 0f,
                Multiplicity = FuNodalMultiplicity.Many
            };
            AddPort(portOut);
        }


public override void OnDraw(FuLayout layout)
{
    string outType = GetPortType("Out");
    switch (outType)
    {
        case "core/float":
            float fVal = GetPortValue<float>("Out", 0f);
            layout.DisableNextElement();
            layout.Drag("##" + Id, ref fVal);
            break;
        case "core/v2":
            Vector2 v2Val = GetPortValue<Vector2>("Out", Vector2.zero);
            layout.DisableNextElement();
            layout.Drag("##" + Id, ref v2Val);
            break;
        case "core/v3":
            Vector3 v3Val = GetPortValue<Vector3>("Out", Vector3.zero);
            layout.DisableNextElement();
            layout.Drag("##" + Id, ref v3Val);
            break;
        case "core/v4":
            Vector4 v4Val = GetPortValue<Vector4>("Out", Vector4.zero);
            layout.DisableNextElement();
            layout.Drag("##" + Id, ref v4Val);
            break;
    }
}



public override void SetDefaultValues(FuNodalPort port)
{
    port.DataType = "core/float";
    port.Data = 0f;
}

    }
}
