using Fu;
using Fu.Core;
using Fu.Framework;
using System;
using UnityEngine;

public class PopupsWindow : MonoBehaviour
{
    void Awake()
    {
        registerPopupsUIWindow();
    }

    /// <summary>
    /// Register a window that draw some buttons to text any Fugui popup types
    /// Modals, Notifications, ContextMenu, Custom Popup Window, PopupMessage
    /// </summary>
    private void registerPopupsUIWindow()
    {
        // create some context menu items
        var someContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0", () => { Debug.Log("Action 0 Lvl 0"); })
            .AddItem("Action 1 Lvl 0", () => { Debug.Log("Action 1 Lvl 0"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1", () => { Debug.Log("Action 0 Lvl 1"); })
            .AddItem("Action 1 Lvl 1", () => { Debug.Log("Action 1 Lvl 1"); })
            .EndChild()
            .Build();

        // create some more context menu items
        var someMoreContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0 : extra", () => { Debug.Log("Action 0 Lvl 0 : extra"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1 : extra", () => { Debug.Log("Action 0 Lvl 1 : extra"); })
            .EndChild()
            .Build();

        // create yet another context menu item
        var yetAnotherContextMenuItem = FuContextMenuBuilder.Start()
            .AddItem("This is a very special listbox", "some shortcut", () => { Debug.Log("click on my very special listbox !"); })
            .Build();

        new FuWindowDefinition(FuWindowsNames.Popups, "Popups Demo", (window) =>
        {
            using (FuPanel panel = new FuPanel("popupWindowPanel", FuStyle.Unpadded))
            {
                using (FuLayout layout = new FuLayout())
                {
                    layout.Collapsable("Modals", () =>
                    {
                        if (layout.Button("Theme Modal"))
                        {
                            Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.Medium);
                        }

                        layout.SetNextElementToolTip("Info style tooltip", "Success style tooltip", "Warning style tooltip", "Danger style tooltip");
                        layout.SetNextElementToolTipStyles(FuTextStyle.Info, FuTextStyle.Success, FuTextStyle.Warning, FuTextStyle.Danger);
                        if (layout.Button("Info modal", FuButtonStyle.Info))
                        {
                            Fugui.ShowInfo("This is an Information", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.Text("This is a nomal text");
                                    layout.Text("This is an info text", FuTextStyle.Info);
                                }
                            }, FuModalSize.Medium);
                        }

                        if (layout.Button("Success modal", FuButtonStyle.Success))
                        {
                            Fugui.ShowSuccess("This is a Success", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.Text("This is a nomal text");
                                    layout.Text("This is a success text", FuTextStyle.Success);
                                }
                            }, FuModalSize.Medium);
                        }

                        if (layout.Button("Warning modal", FuButtonStyle.Warning))
                        {
                            Fugui.ShowWarning("This is a Warning", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.Text("This is a nomal text");
                                    layout.Text("This is a warning text", FuTextStyle.Warning);
                                }
                            }, FuModalSize.Medium);
                        }

                        if (layout.Button("Danger modal", FuButtonStyle.Danger))
                        {
                            Fugui.ShowDanger("This is a Danger", () =>
                            {
                                using (FuLayout layout = new FuLayout())
                                {
                                    layout.Text("This is a nomal text");
                                    layout.Text("This is a danger text", FuTextStyle.Danger);
                                }
                            }, FuModalSize.Medium);
                        }
                    });

                    layout.Collapsable("Notifications", () =>
                    {
                        layout.SetNextElementToolTipWithLabel("Change this flag to set the Fugui notify system anchor");
                        layout.ComboboxEnum<FuOverlayAnchorLocation>("Notify Anchor", (anchor) =>
                        {
                            Fugui.Settings.NotificationAnchorPosition = (FuOverlayAnchorLocation)anchor;
                        }, () => Fugui.Settings.NotificationAnchorPosition);
                        layout.Separator();

                        foreach (StateType type in Enum.GetValues(typeof(StateType)))
                        {
                            if (layout.Button("Notify " + type, FuButtonStyle.GetStyleForState(type)))
                            {
                                Fugui.Notify(type.ToString(), "This is a test " + type + " small notification.", type);
                            }
                        }
                        layout.Separator();

                        foreach (StateType type in Enum.GetValues(typeof(StateType)))
                        {
                            if (layout.Button("Notify long " + type, FuButtonStyle.GetStyleForState(type)))
                            {
                                Fugui.Notify(type.ToString(), "This is a test " + type + " notification. it's a quite long text for a notification but I have to test that the text wrapping don't mess with my notification panel height calculation.", type);
                            }
                        }
                        layout.Separator();

                        foreach (StateType type in Enum.GetValues(typeof(StateType)))
                        {
                            if (layout.Button("Notify title " + type, FuButtonStyle.GetStyleForState(type)))
                            {
                                Fugui.Notify("this is a type " + type.ToString(), null, type);
                            }
                        }
                        layout.Separator();

                        foreach (StateType type in Enum.GetValues(typeof(StateType)))
                        {
                            if (layout.Button("Notify message " + type, FuButtonStyle.GetStyleForState(type)))
                            {
                                Fugui.Notify(null, "this is a type " + type.ToString(), type);
                            }
                        }
                    });

                    Fugui.PushContextMenuItems(someContextMenuItems);
                    layout.Collapsable("Context menu", () =>
                    {
                        Fugui.PushContextMenuItem("you clic the text !", () =>
                        {
                            Debug.Log("text click !");
                        });
                        layout.Text("Right click me");
                        Fugui.PopContextMenuItems();

                        Fugui.PushContextMenuItems(someMoreContextMenuItems);
                        if (layout.Button("click me !"))
                        {
                            Fugui.TryOpenContextMenu();
                        }

                        Fugui.PushContextMenuItems(yetAnotherContextMenuItem);
                        layout.FramedText("I have extra item");
                        Fugui.PopContextMenuItems();

                        Fugui.PopContextMenuItems();
                    });
                    Fugui.TryOpenContextMenuOnWindowClick();
                    Fugui.PopContextMenuItems();
                }
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);
    }
}