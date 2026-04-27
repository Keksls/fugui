using System;

namespace Fu
{
    /// <summary>
    /// Defines the IStandalone File Browser contract.
    /// </summary>
    public interface IStandaloneFileBrowser
    {
        #region Methods
        /// <summary>
        /// Returns the open file panel result.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="extensions">The extensions value.</param>
        /// <param name="multiselect">The multiselect value.</param>
        /// <returns>The result of the operation.</returns>
        string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect);
        /// <summary>
        /// Returns the open folder panel result.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="multiselect">The multiselect value.</param>
        /// <returns>The result of the operation.</returns>
        string[] OpenFolderPanel(string title, string directory, bool multiselect);
        /// <summary>
        /// Returns the save file panel result.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="defaultName">The default Name value.</param>
        /// <param name="extensions">The extensions value.</param>
        /// <returns>The result of the operation.</returns>
        string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions);

        /// <summary>
        /// Runs the open file panel async workflow.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="extensions">The extensions value.</param>
        /// <param name="multiselect">The multiselect value.</param>
        /// <param name="cb">The cb value.</param>
        void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb);
        /// <summary>
        /// Runs the open folder panel async workflow.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="multiselect">The multiselect value.</param>
        /// <param name="cb">The cb value.</param>
        void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb);
        /// <summary>
        /// Runs the save file panel async workflow.
        /// </summary>
        /// <param name="title">The title value.</param>
        /// <param name="directory">The directory value.</param>
        /// <param name="defaultName">The default Name value.</param>
        /// <param name="extensions">The extensions value.</param>
        /// <param name="cb">The cb value.</param>
        void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb);
        #endregion
    }
}