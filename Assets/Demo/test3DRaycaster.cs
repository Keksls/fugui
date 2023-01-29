using Fu.Core;
using UnityEngine;

public class Test3DRaycaster : MonoBehaviour
{
    private void Start()
    {
        FuRaycasting.RegisterRaycaster(new FuRaycaster("3DRaycasterTest",
            () => new Ray(transform.position, transform.forward),
            () => Input.GetKeyDown(KeyCode.C),
            () => Input.GetKeyDown(KeyCode.V),
            () => Input.GetKeyDown(KeyCode.B),
            () => Input.mouseScrollDelta.y,
            () => true));
    }
}