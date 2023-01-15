using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fugui.Framework
{
    internal class UIObjectDescription
    {
        internal Dictionary<string, UIField> Fields = null;
        private static Dictionary<HashSet<Type>, Func<FieldInfo, UIField>> _uiFieldBinding = null;

        internal UIObjectDescription()
        {
            // bind Type/UIField mapping
            if (_uiFieldBinding == null)
            {
                bindFieldTypeDictionary();
            }
        }

        /// <summary>
        /// bind te dictionary that represend mapping between field type and UIField type
        /// </summary>
        private static void bindFieldTypeDictionary()
        {
            _uiFieldBinding = new Dictionary<HashSet<Type>, Func<FieldInfo, UIField>>();

            // bool
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(bool) }, (fi) =>
                    {
                        if (fi.IsDefined(typeof(Toggle)))
                        {
                            return new ToggleField(fi);
                        }
                        else
                        {
                            return new CheckboxField(fi);
                        }
                    });

            // numbers
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(byte),
                    typeof(short),
                    typeof(ushort),
                    typeof(int),
                    typeof(uint),
                    typeof(long),
                    typeof(ulong),
                    typeof(float),
                    typeof(double),
                    typeof(decimal)}, (fi) =>
                    {
                        if (fi.IsDefined(typeof(Slider)))
                        {
                            return new SliderField(fi);
                        }
                        else
                        {
                            return new DragField(fi);
                        }
                    });

            // enum
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(Enum) }, (fi) =>
                    {
                        return new ComboboxField(fi);
                    });

            // Vector2
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(Vector2) }, (fi) =>
                    {
                        return new DragField(fi);
                    });

            // Vector3 and 4
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(Vector3),
                    typeof(Vector4) }, (fi) =>
                    {
                        if (fi.IsDefined(typeof(ColorPicker)))
                        {
                            return new ColorPickerField(fi);
                        }
                        else
                        {
                            return new DragField(fi);
                        }
                    });

            // Color
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(Color) }, (fi) =>
                    {
                        return new ColorPickerField(fi);
                    });

            // Text
            _uiFieldBinding.Add(new HashSet<Type>() {
                    typeof(string) }, (fi) =>
                    {
                        return new TextField(fi);
                    });
        }

        /// <summary>
        /// Bind fields from a given object of type T to UIField objects.
        /// </summary>
        /// <typeparam name="T">The type of the object to bind fields from.</typeparam>
        private void bindFromObject<T>()
        {
            // fields already binded
            if (Fields != null)
            {
                return;
            }
            Fields = new Dictionary<string, UIField>();

            // get type of T
            Type type = typeof(T);
            // get all field of T
            FieldInfo[] fields = type.GetFields();
            // iterate on fields
            foreach (FieldInfo field in fields)
            {
                UIField uiField = null;
                Type fieldType = field.FieldType;

                // check whatever the field need to be switched (hiden => not displayed)
                if (field.IsDefined(typeof(Hiden)))
                {
                    continue;
                }

                // check whatever the field is an enum => to get base enum type (Enum)
                if (fieldType.IsEnum)
                {
                    uiField = getUIField(typeof(Enum), field);
                }
                // check whatever the type can be displayed
                else
                {
                    uiField = getUIField(fieldType, field);
                }

                // owether, that mean the type of the fiels can't be edited so we just dislay value as string
                if (uiField == null)
                {
                    uiField = new NonEditableField(field);
                }

                // add field to dic
                Fields.Add(field.Name, uiField);
            }
        }

        /// <summary>
        /// Get instance of UIField object for a giver type and fileInfo
        /// </summary>
        /// <param name="type">Type to get field on</param>
        /// <param name="field">field to get UIField on</param>
        /// <returns>UIField if bindable</returns>
        private UIField getUIField(Type type, FieldInfo field)
        {
            foreach (var pair in _uiFieldBinding)
            {
                if (pair.Key.Contains(type))
                {
                    return pair.Value?.Invoke(field);
                }
            }
            return null;
        }

        /// <summary>
        /// Draw the object instance
        /// </summary>
        /// <typeparam name="T">Type of the object to draw</typeparam>
        /// <param name="grid">grid to draw object in</param>
        /// <param name="objectInstance">object instance to draw</param>
        /// <returns>true if some value has just been edited</returns>
        internal bool DrawObject<T>(UIGrid grid, T objectInstance)
        {
            // check whatever object Type is already binded
            if (Fields == null)
            {
                // bind the object of not already
                bindFromObject<T>();
            }

            // check whatever objectInstance is null (it will fail so let's log error and return
            if (objectInstance == null)
            {
                Debug.LogError("You are trying to display a null object");
                return false;
            }

            // draw each binded fields
            bool updated = false;
            foreach (var pair in Fields)
            {
                updated |= pair.Value.Draw(grid, objectInstance);
            }
            // return whatever any field has just been updated
            return updated;
        }

        /// <summary>
        /// Get NumericFieldType enum from Type
        /// </summary>
        /// <param name="type">Type to get enum on</param>
        /// <returns>Enum that represent the Type</returns>
        internal static NumericFieldType GetNumericFieldType(Type type)
        {
            if(type == typeof(byte))
            {
                return NumericFieldType.Byte;
            }
            else if (type == typeof(short))
            {
                return NumericFieldType.Short;
            }
            else if (type == typeof(ushort))
            {
                return NumericFieldType.UShort;
            }
            else if (type == typeof(int))
            {
                return NumericFieldType.Int;
            }
            else if (type == typeof(float))
            {
                return NumericFieldType.Float;
            }
            else if (type == typeof(Vector2))
            {
                return NumericFieldType.Vector2;
            }
            else if (type == typeof(Vector3))
            {
                return NumericFieldType.Vector3;
            }
            else if (type == typeof(Vector4))
            {
                return NumericFieldType.Vector4;
            }

            return NumericFieldType.None;
        }
    }

    internal enum NumericFieldType
    {
        None,
        Byte,
        Short,
        UShort,
        Int,
        Float,
        Vector2,
        Vector3,
        Vector4
    }
}