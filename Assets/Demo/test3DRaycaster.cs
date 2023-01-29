using Fu.Core;
using UnityEngine;

public class Test3DRaycaster : MonoBehaviour
{
    public KeyCode MouseButton0Key = KeyCode.C;
    public KeyCode MouseButton1Key = KeyCode.V;
    public KeyCode MouseButton2Key = KeyCode.B;
    public KeyCode MouseScrollUpKey = KeyCode.KeypadPlus;
    public KeyCode MouseScrollDownKey = KeyCode.KeypadMinus;
    public float ScrollForce = 0.1f;

    private void Start()
    {
        FuRaycasting.RegisterRaycaster(new FuRaycaster("3DRaycasterTest",
            () => new Ray(transform.position, transform.forward),
            () => Input.GetKey(MouseButton0Key),
            () => Input.GetKey(MouseButton1Key),
            () => Input.GetKey(MouseButton2Key),
            () => 0f + (Input.GetKeyDown(MouseScrollUpKey) ? ScrollForce : 0f) - (Input.GetKeyDown(MouseScrollDownKey) ? ScrollForce : 0f),
            () => true));
    }
}