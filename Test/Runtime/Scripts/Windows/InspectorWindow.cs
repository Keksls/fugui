using Fu.Core;
using Fu.Framework;
using UnityEngine;

public class InspectorWindow : MonoBehaviour
{
    public Camera Camera;
    public Test3DRaycaster Raycaster;

    private void Awake()
    {
        registerInspectorWindow();
    }

    /// <summary>
    /// Register the FuWindowDefinition of the Inspector window
    /// </summary>
    private void registerInspectorWindow()
    {
        new FuWindowDefinition(FuWindowsNames.Inspector, "Inspector", (window) =>
        {
            using (new FuPanel("demoContainer", FuStyle.Unpadded))
            {
                using (var layout = new FuLayout())
                {
                    // raycaster transform
                    layout.Collapsable("Transform (raycaster)", () =>
                    {
                        using (FuGrid grid = new FuGrid("transformGrid", rowsPadding: 1f, outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(20f);
                            Vector3 pos = Raycaster.transform.position;
                            grid.SetNextElementToolTip("x parameter of the position", "y parameter of the position", "z parameter of the position");
                            if (grid.Drag(Icons.Position + " Position", ref pos, "X", "Y", "Z", -100f, 100f))
                            {
                                Raycaster.transform.position = pos;
                            }

                            grid.SetNextElementToolTip("x parameter of the rotation", "y parameter of the rotation", "z parameter of the rotation");
                            Vector3 rot = Raycaster.transform.localEulerAngles;
                            if (grid.Drag(Icons.Rotate + " Rotation", ref rot, "X", "Y", "Z", -360f, 360f))
                            {
                                Raycaster.transform.localEulerAngles = rot;
                            }

                            grid.SetNextElementToolTip("x parameter of the scale", "y parameter of the scale", "z parameter of the scale");
                            Vector3 scale = Raycaster.transform.localScale;
                            if (grid.Drag("Scale", ref scale, "X", "Y", "Z", 0.1f, 2f))
                            {
                                Raycaster.transform.localScale = scale;
                            }
                        }
                    }, 8f);

                    // camera settings
                    layout.Collapsable("Camera", () =>
                    {
                        using (FuGrid grid = new FuGrid("cameraGrid", outterPadding: 8f))
                        {
                            // camera clear flags
                            grid.SetMinimumLineHeight(22f);
                            grid.SetNextElementToolTipWithLabel("Clear flag of the camera");
                            grid.ComboboxEnum<CameraClearFlags>("Clear Flags", (CameraClearFlags) =>
                            {
                                Camera.clearFlags = (CameraClearFlags)CameraClearFlags;
                            }, () => { return Camera.clearFlags; });

                            // camera field of view
                            float FOV = Camera.fieldOfView;
                            grid.SetNextElementToolTipWithLabel("Field of View (FOV) of the camera");
                            if (grid.Slider("Field of view", ref FOV))
                            {
                                Camera.fieldOfView = FOV;
                            }

                            // Use physical camera
                            bool physicalCamera = Camera.usePhysicalProperties;
                            grid.SetNextElementToolTip("Whatever the main camera use physical properties");
                            if (grid.CheckBox("Physical Camera", ref physicalCamera))
                            {
                                Camera.usePhysicalProperties = physicalCamera;
                            }

                            // background color
                            grid.SetNextElementToolTipWithLabel("Background Color of the camera (only if Clear Flag is on 'Solid Color')");
                            if (Camera.clearFlags != CameraClearFlags.SolidColor)
                            {
                                grid.DisableNextElement();
                            }
                            Vector4 color = Camera.backgroundColor;
                            if (grid.ColorPicker("BG Color", ref color))
                            {
                                Camera.backgroundColor = color;
                            }
                        }
                    }, 8f);
                }
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);
    }
}