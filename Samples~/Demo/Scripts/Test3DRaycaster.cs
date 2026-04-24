using Fu;
using ImGuiNET;
using UnityEngine;

public class Test3DRaycaster : MonoBehaviour
{
    public KeyCode MouseButton0Key = KeyCode.C;
    public KeyCode MouseButton1Key = KeyCode.V;
    public KeyCode MouseButton2Key = KeyCode.B;
    public KeyCode MouseScrollUpKey = KeyCode.KeypadPlus;
    public KeyCode MouseScrollDownKey = KeyCode.KeypadMinus;
    public float ScrollForce = 1f;

    private void OnEnable()
    {
        FuRaycasting.RegisterRaycaster(new FuRaycaster("3DRaycasterTest",
            () => new Ray(transform.position, transform.forward),
            () => ImGui.GetIO().MouseDown[0],
            () => ImGui.GetIO().MouseDown[1],
            () => ImGui.GetIO().MouseDown[2],
            () => 0f,
            () => true));
    }

    private void OnDisable()
    {
        FuRaycasting.UnRegisterRaycaster("3DRaycasterTest");
    }
}