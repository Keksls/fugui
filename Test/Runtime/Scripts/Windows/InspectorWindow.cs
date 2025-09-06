using Fu;
using Fu.Framework;
using ImGuiNET;
using UnityEngine;

public class InspectorWindow : FuWindowBehaviour
{
    public Camera Camera;
    public Test3DRaycaster Raycaster;
    public CameraWindow CameraWindow;

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        using (new FuPanel("demoContainer", FuStyle.Unpadded))
        {
            // raycaster transform
            layout.Collapsable("Transform (raycaster)", () =>
            {
                using (FuGrid grid = new FuGrid("transformGrid", rowsPadding: 1f, outterPadding: 8f))
                {
                    grid.SetMinimumLineHeight(20f);
                    Vector3 pos = Raycaster.transform.position;
                    grid.SetNextElementToolTip("x parameter of the position", "y parameter of the position", "z parameter of the position");
                    if (grid.Drag(Icons.Edit_duotone + " Position", ref pos, "X", "Y", "Z", -100f, 100f))
                    {
                        Raycaster.transform.position = pos;
                    }

                    grid.SetNextElementToolTip("x parameter of the rotation", "y parameter of the rotation", "z parameter of the rotation");
                    Vector3 rot = Raycaster.transform.localEulerAngles;
                    if (grid.Drag(Icons.Displacement_duotone + " Rotation", ref rot, "X", "Y", "Z", -360f, 360f))
                    {
                        Raycaster.transform.localEulerAngles = rot;
                    }

                    grid.SetNextElementToolTip("x parameter of the scale", "y parameter of the scale", "z parameter of the scale");
                    Vector3 scale = Raycaster.transform.localScale;
                    if (grid.Drag(Icons.Link_duotone + " Scale", ref scale, "X", "Y", "Z", 0.1f, 2f))
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
            }, FuButtonStyle.Collapsable, 8f, true, 22f, (h) =>
            {
                bool camEnabled = CameraWindow.enabled;
                layout.CenterNextItemV(16f * Fugui.Scale, h);
                if (layout.CheckBox("##cmEnbldChkCsbl", ref camEnabled))
                {
                    CameraWindow.enabled = camEnabled;
                }
            }, 22f, (h) =>
            {
                string cameraSettingsPopupID = "cmPppStng";
                Fugui.PushFont(18, FontType.Regular);
                layout.CenterNextItemV(Icons.MenuDots, h, false);
                if (layout.ClickableText(Icons.MenuDots))
                {
                    Fugui.OpenPopUp(cameraSettingsPopupID, () =>
                    {
                        layout.Spacing();
                        using (FuGrid grid = new FuGrid("cmStngGrd", width: 196f, outterPadding: 8f))
                        {
                            grid.Slider("Hit force", ref CameraWindow.HitForce, 10f, 500f, format: "%.0f N");
                        }
                        layout.Spacing();
                    });
                }
                Fugui.PopFont();
                Fugui.DrawPopup(cameraSettingsPopupID);
            });
        }
    }
}