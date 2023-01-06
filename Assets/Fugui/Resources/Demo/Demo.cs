using Fugui.Core;
using Fugui.Framework;
using UnityEngine;

public class Demo : MonoBehaviour
{
    void Awake()
    {
        DockingLayoutManager.OnDockLayoutInitialized += DockingLayoutManager_OnDockLayoutInitialized;
    }

    private void DockingLayoutManager_OnDockLayoutInitialized()
    {
        UIWindowDefinition winDef = new UIWindowDefinition(UIWindowName.None, "Demo", drawWindow);
        UIWindow window = new(winDef);
        window.TryAddToContainer(FuGui.MainContainer);
    }

    private void drawWindow(UIWindow window)
    {
       
    }
}