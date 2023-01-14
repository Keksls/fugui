using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fugui.Framework
{
    public class UIAutoGridDescription
    {
        public Dictionary<string, UIAutoGridField> Fields;
        private static Dictionary<Type, Func<FieldInfo, UIAutoGridField>> binding;

        public UIAutoGridDescription()
        {
            // bind default 
            if (binding == null)
            {
                binding = new Dictionary<Type, Func<FieldInfo, UIAutoGridField>>();

                // bool
                binding.Add(typeof(bool), (fi) =>
                {
                    if (fi.IsDefined(typeof(ToggleAtttribute)))
                    {
                        return new ToggleField(fi);
                    }
                    else
                    {
                        return new CheckboxField(fi);
                    }
                });

                // int
                binding.Add(typeof(int), (fi) =>
                {
                    if (fi.IsDefined(typeof(SliderIntAtttribute)))
                    {
                        return new SliderIntField(fi);
                    }
                    else
                    {
                        return new DragIntField(fi);
                    }
                });

                // float
                binding.Add(typeof(float), (fi) =>
                {
                    if (fi.IsDefined(typeof(SliderFloatAtttribute)))
                    {
                        return new SliderFloatField(fi);
                    }
                    else
                    {
                        return new DragFloatField(fi);
                    }
                });

                // enum
                binding.Add(typeof(Enum), (fi) =>
                {
                    return new ComboboxField(fi);
                });

                // Vector2
                binding.Add(typeof(Vector2), (fi) =>
                {
                    return new DragVector2Field(fi);
                });

                // Vector3
                binding.Add(typeof(Vector3), (fi) =>
                {
                    return new DragVector3Field(fi);
                });

                // Vector4
                binding.Add(typeof(Vector4), (fi) =>
                {
                    return new DragVector4Field(fi);
                });

                // Text
                binding.Add(typeof(string), (fi) =>
                {
                    return new TextField(fi);
                });
            }
        }

        public UIAutoGridDescription BindFromObject<T>()
        {
            if (Fields != null)
            {
                return this;
            }
            Fields = new Dictionary<string, UIAutoGridField>();

            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                UIAutoGridField uiField = null;
                Type fieldType = field.FieldType;
                if (fieldType.IsEnum)
                {
                    uiField = binding[typeof(Enum)].Invoke(field);
                }
                else if (binding.ContainsKey(fieldType))
                {
                    uiField = binding[fieldType].Invoke(field);
                }

                if (uiField != null)
                {
                    Fields.Add(field.Name, uiField);
                }
            }
            return this;
        }

        public bool DrawObject<T>(UIGrid grid, T objectInstance)
        {
            if (objectInstance == null)
            {
                return false;
            }

            bool updated = false;
            foreach (var pair in Fields)
            {
                updated |= pair.Value.Draw(grid, objectInstance);
            }
            return updated;
        }
    }
}