using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Docking Layout Definition type.
    /// </summary>
    public class FuDockingLayoutDefinition
    {
        #region State
        /// <summary>
        /// The name of the dock space
        /// </summary>
        public string Name;
        /// <summary>
        /// The unique identifier of the dock space
        /// </summary>
        public uint ID;
        /// <summary>
        /// The proportion of the dock space relative to its parent
        /// </summary>
        public float Proportion;
        /// <summary>
        /// The orientation of the dock space
        /// </summary>
        public UIDockSpaceOrientation Orientation;
        /// <summary>
        /// A list of child dock spaces
        /// </summary>
        [JsonProperty]
        public List<FuDockingLayoutDefinition> Children;
        /// <summary>
        /// A list of binded windowsdefintion
        /// </summary>
        [JsonProperty]
        public List<ushort> WindowsDefinition;
        /// <summary>
        /// Whatever this layout auto hide topbars
        /// </summary>
        public bool AutoHideTopBar;
        /// <summary>
        /// Custom var that you can use to store a flag that identify the type of this layout (create your own enum if needed)
        /// </summary>
        public byte LayoutType;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor, used for serialization purposes
        /// </summary>
        public FuDockingLayoutDefinition()
        {
        }

        /// <summary>
        /// Constructor that accepts 4 parameters: name, id, proportion, and orientation
        /// </summary>
        /// <param name="name"> The name of the dock space</param>
        /// <param name="id"> The unique identifier of the dock space</param>
        /// <param name="proportion"> The proportion of the dock space relative to its parent</param>
        /// <param name="orientation"> The orientation of the dock space</param>
        public FuDockingLayoutDefinition(string name, uint id, float proportion, UIDockSpaceOrientation orientation)
        {
            Name = name;
            ID = id;
            Proportion = proportion;
            Orientation = orientation;
            Children = new List<FuDockingLayoutDefinition>();
            WindowsDefinition = new List<ushort>();
        }

        /// <summary>
        /// Constructor that accepts 2 parameters: name and id
        /// </summary>
        /// <param name="name"> The name of the dock space</param>
        /// <param name="id"> The unique identifier of the dock space</param>
        public FuDockingLayoutDefinition(string name, uint id)
        {
            Name = name;
            ID = id;
            Proportion = 0.5f;
            Orientation = UIDockSpaceOrientation.None;
            Children = new List<FuDockingLayoutDefinition>();
            WindowsDefinition = new List<ushort>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Method that serializes the dock space definition to a JSON string
        /// </summary>
        /// <param name="dockspaceDefinition"> The dock space definition to serialize</param>
        /// <returns> A JSON string representing the dock space definition</returns>
        public static string Serialize(FuDockingLayoutDefinition dockspaceDefinition)
        {
            return JsonConvert.SerializeObject(dockspaceDefinition);
        }

        /// <summary>
        /// Method that writes the serialized dock space definition to a file
        /// </summary>
        /// <param name="pathFile"> The path to the file containing the serialized dock space definition</param>
        /// <returns> A FuDockingLayoutDefinition object representing the deserialized dock space</returns>
        public static FuDockingLayoutDefinition Deserialize(string pathFile)
        {
            FuDockingLayoutDefinition result = null;

            try
            {
                string json = Fugui.ReadAllText(pathFile);

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                result = JsonConvert.DeserializeObject<FuDockingLayoutDefinition>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(ex.GetBaseException().Message);
            }

            return result;
        }

        /// <summary>
        /// Get all window definitions of this dock space
        /// </summary>
        /// <param name="getOnlyAutoInstantiated">Whatever you only want windows in this layout that will auto instantiated by layout</param>
        /// <returns>list of all window names</returns>
        internal List<FuWindowName> GetAllWindowsNames(bool getOnlyAutoInstantiated)
        {
            List<FuWindowName> windows = new List<FuWindowName>();

            foreach (ushort windowID in WindowsDefinition)
            {
                if (!FuWindowNameProvider.GetAllWindowNames().TryGetValue(windowID, out FuWindowName windowName))
                {
                    continue;
                }

                if (getOnlyAutoInstantiated && windowName.AutoInstantiateWindowOnlayoutSet || !getOnlyAutoInstantiated)
                {
                    windows.Add(windowName);
                }
            }

            foreach (var child in Children)
            {
                windows.AddRange(child.GetAllWindowsNames(getOnlyAutoInstantiated));
            }

            return windows;
        }

        #endregion
    }

    /// <summary>
    /// Enum for setting the orientation of a dock space
    /// </summary>
    public enum UIDockSpaceOrientation
    {
        /// <summary>
        /// None orientation
        /// </summary>
        None,
        /// <summary>
        /// Horizontal orientation
        /// </summary>
        Horizontal,
        /// <summary>
        /// Vertical orientation
        /// </summary>
        Vertical
    }

}
