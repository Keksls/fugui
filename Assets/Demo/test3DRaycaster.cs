using Fugui.Core;
using UnityEngine;

public class test3DRaycaster : MonoBehaviour
{
    private void Start()
    {
        InputManager.RegisterRaycaster(new FuguiRaycaster("3DRaycasterTest",
            () => new Ray(transform.position, transform.forward),
            () => Input.GetKeyDown(KeyCode.C),
            () => Input.GetKeyDown(KeyCode.V),
            () => Input.GetKeyDown(KeyCode.B),
            () => Input.mouseScrollDelta.y,
            () => true));
    }
}