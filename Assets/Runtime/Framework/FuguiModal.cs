using Fugui.Framework;
using ImGuiNET;
using System;

namespace Assets.Runtime.Framework
{
    public static class FuguiModal
    {
        private static bool showModal = false;
        private static string modalTitle;
        private static Action modalBody;
        private static UIModalButton[] modalButtons;

        /// <summary>
        /// Show a modal with a custom title, body, and buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        /// <param name="buttons">List of buttons in the modal, each button contains a text and callback</param>
        public static void ShowModal(string title, Action body, params UIModalButton[] buttons)
        {
            modalTitle = title; //store the title
            modalBody = body; //store the body
            modalButtons = buttons; //store the buttons
            showModal = true; //set showModal to true to show the modal
        }

        /// <summary>
        /// Hide the currently shown modal
        /// </summary>
        public static void HideModal()
        {
            showModal = false; //set showModal to false to hide the modal
        }

        /// <summary>
        /// Render the currently shown modal
        /// </summary>
        public static void RenderModal()
        {
            if (showModal)
            {
                using (new UIContainer("FuguiModalContainer"))
                {
                    using (UILayout layout = new UILayout())
                    {
                        ImGui.OpenPopup(modalTitle); //open the modal with the stored title
                        if (ImGui.BeginPopupModal(modalTitle))
                        {
                            if (modalBody != null)
                            {
                                modalBody(); //call the stored body callback
                                ImGui.Separator();
                            }
                            // draw buttons
                            foreach (var button in modalButtons)
                            {
                                button.Draw(layout);
                                layout.SameLine();
                            }
                            ImGui.EndPopup(); //end the modal
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show a modal with yes and no buttons
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="callback">Callback to be called when the yes button is pressed</param>
        /// <param name="yesButtonText">text of the yes button</param>
        /// <param name="noButtonText">text of the no button</param>
        public static void ShowYesNoModal(string title, Action<bool> callback, string yesButtonText = "Yes", string noButtonText = "No")
        {
            //call the ShowModal method with the title and buttons
            ShowModal(title, null,
                new UIModalButton(yesButtonText, () =>
                {
                    HideModal();
                    callback?.Invoke(true);
                }, UIButtonStyle.Default),
                new UIModalButton(noButtonText, () =>
                {
                    HideModal();
                    callback?.Invoke(false);
                }, UIButtonStyle.Default));
        }

        /// <summary>
        /// Show a modal with an info box and an ok button
        /// </summary>
        /// <param name="title">Title of the modal</param>
        /// <param name="body">Body of the modal</param>
        public static void ShowInfoBoxModal(string title, Action body)
        {
            //call the ShowModal method with the title, body, and buttons
            ShowModal(title, body, new UIModalButton("OK", HideModal, UIButtonStyle.Default));
        }
    }

    public struct UIModalButton
    {
        public string text;
        public Action callback;
        public UIButtonStyle style;

        public UIModalButton(string text, Action callback, UIButtonStyle style)
        {
            this.text = text;
            this.callback = callback;
            this.style = style;
        }

        public void Draw(UILayout layout)
        {
            if (layout.Button(text, UIButtonStyle.AutoSize, style))
            {
                callback();
            }
        }
    }
}