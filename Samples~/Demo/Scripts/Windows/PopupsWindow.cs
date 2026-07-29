using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the Popups Window type.
/// </summary>
public class PopupsWindow : FuWindowBehaviour
{
    #region State
    [SerializeField] private Texture2D _testImage;
    private List<FuContextMenuItem> someContextMenuItems;
    private List<FuContextMenuItem> someMoreContextMenuItems;
    private List<FuContextMenuItem> yetAnotherContextMenuItem;
    #endregion

    #region Methods
    /// <summary>
    /// Runs the awake workflow.
    /// </summary>
    private void Awake()
    {
        // create some context menu items
        someContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0", () => { Debug.Log("Action 0 Lvl 0"); })
            .AddItem("Action 1 Lvl 0", () => { Debug.Log("Action 1 Lvl 0"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1", () => { Debug.Log("Action 0 Lvl 1"); })
            .AddItem("Action 1 Lvl 1", () => { Debug.Log("Action 1 Lvl 1"); })
            .EndChild()
            .SetTitle("Context menu title")
            .Build();

        // create some more context menu items
        someMoreContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0 : extra", () => { Debug.Log("Action 0 Lvl 0 : extra"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1 : extra", () => { Debug.Log("Action 0 Lvl 1 : extra"); })
            .EndChild()
            .Build();

        // create yet another context menu item
        yetAnotherContextMenuItem = FuContextMenuBuilder.Start()
            .AddItem("This is a very special listbox", () => { Debug.Log("click on my very special listbox !"); }, "some shortcut")
            .SetTitle("Section title")
            .AddItem("", () =>
            {
                Fugui.Notify("Image clicked!", "Image clicked in contextual menu", StateType.Info);
            }, image: _testImage, imageSize: new FuElementSize(32f, 32f))
            .AddItem("Image Item", () =>
            {
                Fugui.Notify("Image item clicked!", "Image clicked in contextual menu", StateType.Info);
            }, image: _testImage)
            .BeginChild("Image Parent", _testImage)
            .SetTitle("Sub menu title")
            .AddItem("Image Child", () =>
            {
                Fugui.Notify("Image child clicked!", "Image clicked in contextual menu", StateType.Info);
            }, image: _testImage)
            .EndChild()
            .Build();
    }

    /// <summary>
    /// Handles the UI event.
    /// </summary>
    /// <param name="window">The window value.</param>
    public override void OnUI(FuWindow window)
    {
        using (FuPanel panel = new FuPanel("popupWindowPanel", FuStyle.Unpadded))
        {
            Fugui.Layout.Collapsable("Modals", () =>
            {
                if (Fugui.Layout.Button("Theme Modal"))
                {
                    Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.Medium);
                }

                Fugui.Layout.SetNextElementToolTip("Info style tooltip", "Success style tooltip", "Warning style tooltip", "Danger style tooltip");
                Fugui.Layout.SetNextElementToolTipStyles(FuTextStyle.Info, FuTextStyle.Success, FuTextStyle.Warning, FuTextStyle.Danger);
                if (Fugui.Layout.Button("Info modal", FuButtonStyle.Info))
                {
                    Fugui.ShowInfo("This is an Information", () =>
                    {
                        Fugui.Layout.Text("This is a nomal text");
                        Fugui.Layout.Text("This is an info text", FuTextStyle.Info);
                    }, FuModalSize.Medium);
                }

                if (Fugui.Layout.Button("Success modal", FuButtonStyle.Success))
                {
                    Fugui.ShowSuccess("This is a Success", () =>
                    {
                        Fugui.Layout.Text("This is a nomal text");
                        Fugui.Layout.Text("This is a success text", FuTextStyle.Success);
                    }, FuModalSize.Medium);
                }

                if (Fugui.Layout.Button("Warning modal", FuButtonStyle.Warning))
                {
                    Fugui.ShowWarning("This is a Warning", () =>
                    {
                        Fugui.Layout.Text("This is a nomal text");
                        Fugui.Layout.Text("This is a warning text", FuTextStyle.Warning);
                    }, FuModalSize.Medium);
                }

                if (Fugui.Layout.Button("Danger modal", FuButtonStyle.Danger))
                {
                    Fugui.ShowDanger("This is a Danger", () =>
                    {
                        Fugui.Layout.Text("This is a nomal text");
                        Fugui.Layout.Text("This is a danger text", FuTextStyle.Danger);
                    }, FuModalSize.Medium);
                }
            });

            Fugui.Layout.Collapsable("Notifications", () =>
            {
                Fugui.Layout.SetNextElementToolTipWithLabel("Change this flag to set the Fugui notify system anchor");
                Fugui.Layout.ComboboxEnum<FuOverlayAnchorLocation>("Notify Anchor", (anchor) =>
                {
                    Fugui.Settings.NotificationAnchorPosition = (FuOverlayAnchorLocation)anchor;
                }, () => Fugui.Settings.NotificationAnchorPosition);
                Fugui.Layout.Separator();

                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (Fugui.Layout.Button("Notify " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(type.ToString(), "This is a test " + type + " small notification.", type);
                    }
                }
                Fugui.Layout.Separator();

                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (Fugui.Layout.Button("Notify long " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(type.ToString(), "This is a test " + type + " notification. it's a quite long text for a notification but I have to test that the text wrapping don't mess with my notification panel height calculation.", type);
                    }
                }
                Fugui.Layout.Separator();

                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (Fugui.Layout.Button("Notify title " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify("this is a type " + type.ToString(), null, type);
                    }
                }
                Fugui.Layout.Separator();

                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (Fugui.Layout.Button("Notify message " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(null, "this is a type " + type.ToString(), type);
                    }
                }
            });

            Fugui.PushContextMenuItems(someContextMenuItems);
            Fugui.Layout.Collapsable("Context menu", () =>
            {
                Fugui.PushContextMenuItem("you clic the text !", () =>
                {
                    Debug.Log("text click !");
                });
                Fugui.Layout.Text("Right click me");
                Fugui.PopContextMenuItems();

                Fugui.PushContextMenuItems(someMoreContextMenuItems);
                if (Fugui.Layout.Button("click me !"))
                {
                    Fugui.TryOpenContextMenu();
                }

                Fugui.PushContextMenuItems(yetAnotherContextMenuItem);
                Fugui.Layout.FramedText("I have extra item");
                Fugui.TryOpenContextMenuOnItemClick();
                Fugui.PopContextMenuItems(2);
            });
            Fugui.TryOpenContextMenuOnWindowClick();
            Fugui.PopContextMenuItems();
        }
    }
    #endregion
}
