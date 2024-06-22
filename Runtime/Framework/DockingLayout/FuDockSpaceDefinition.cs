using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fu.Framework
{
    public class FuDockingLayoutDefinition
    {
        /// <summary>
        /// The name of the dock space
        ///</summary>
        public string Name;
        /// <summary>
        /// The unique identifier of the dock space
        ///</summary>
        public uint ID;
        /// <summary>
        /// The proportion of the dock space relative to its parent
        ///</summary>
        public float Proportion;
        /// <summary>
        /// The orientation of the dock space
        ///</summary>
        public UIDockSpaceOrientation Orientation;
        /// <summary>
        /// A list of child dock spaces
        ///</summary>
        [JsonProperty]
        public List<FuDockingLayoutDefinition> Children;
        /// <summary>
        /// A list of binded windowsdefintion
        ///</summary>
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

        public FuDockingLayoutDefinition()
        {

        }

        //Constructor that accepts 4 parameters: name, id, proportion and orientation
        public FuDockingLayoutDefinition(string name, uint id, float proportion, UIDockSpaceOrientation orientation)
        {
            Name = name;
            ID = id;
            Proportion = proportion;
            Orientation = orientation;
            Children = new List<FuDockingLayoutDefinition>();
            WindowsDefinition = new List<ushort>();
        }

        //Constructor that accepts 2 parameters: name and id, with default values for proportion and orientation
        public FuDockingLayoutDefinition(string name, uint id)
        {
            Name = name;
            ID = id;
            Proportion = 0.5f;
            Orientation = UIDockSpaceOrientation.None;
            Children = new List<FuDockingLayoutDefinition>();
            WindowsDefinition = new List<ushort>();
        }

        //Method that returns the total number of children, including all children of children
        public uint GetTotalChildren()
        {
            uint count = (uint)Children.Count;

            foreach (var child in Children)
            {
                count += child.GetTotalChildren();
            }

            return count;
        }

        //Serialization method
        public static string Serialize(FuDockingLayoutDefinition dockspaceDefinition)
        {
            return JsonConvert.SerializeObject(dockspaceDefinition);
        }

        //Deserialization method
        public static FuDockingLayoutDefinition ReadFromFile(string pathFile)
        {
            FuDockingLayoutDefinition result = null;

            try
            {
                using (StreamReader sr = new StreamReader(pathFile))
                {
                    result = JsonConvert.DeserializeObject<FuDockingLayoutDefinition>(sr.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(ex.GetBaseException().Message);
            }

            return result;
        }

        /// <summary>
        /// Method that search for a child dock space with the specified name in the current dock space and its children recursively
        /// </summary>
        /// <param name="dockspaceName">The name of the dock space to search for</param>
        /// <returns>The dock space with the specified name, or null if not found</returns>
        internal FuDockingLayoutDefinition SearchInChildren(string dockspaceName)
        {
            if (Name == dockspaceName)
            {
                return this;
            }
            else
            {
                foreach (var child in Children)
                {
                    var found = child.SearchInChildren(dockspaceName);

                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Method that search for a child dock space with the specified window def ID in the current dock space and its children recursively
        /// </summary>
        /// <param name="windowDefID">The ID of the window definition</param>
        /// <returns>The dock space with the specified name, or null if not found</returns>
        internal FuDockingLayoutDefinition SearchInChildren(ushort windowDefID)
        {
            if (WindowsDefinition.Contains(windowDefID))
            {
                return this;
            }
            else
            {
                foreach (var child in Children)
                {
                    var found = child.SearchInChildren(windowDefID);

                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Method that removes all entries from the WindowsDefinition dictionary that have the specified window definition ID in the current dock space and its children recursively
        /// </summary>
        /// <param name="windowDefID">The ID of the window definition to remove</param>
        internal void RemoveWindowsDefinitionInChildren(ushort windowDefID)
        {
            WindowsDefinition.Remove(windowDefID);

            foreach (var child in Children)
            {
                child.RemoveWindowsDefinitionInChildren(windowDefID);
            }
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
                if (getOnlyAutoInstantiated && FuDockingLayoutManager.RegisteredWindowsNames[windowID].AutoInstantiateWindowOnlayoutSet || !getOnlyAutoInstantiated)
                {
                    windows.Add(FuDockingLayoutManager.RegisteredWindowsNames[windowID]);
                }
            }

            foreach (var child in Children)
            {
                windows.AddRange(child.GetAllWindowsNames(getOnlyAutoInstantiated));
            }

            return windows;

        }

        internal FuDockingLayoutDefinition GetCopy()
        {
            FuDockingLayoutDefinition clone = new FuDockingLayoutDefinition(Name, ID, Proportion, Orientation);
            clone.WindowsDefinition = new List<ushort>(WindowsDefinition);
            clone.Children = new List<FuDockingLayoutDefinition>();
            foreach (var child in Children)
            {
                clone.Children.Add(child.GetCopy());
            }
            return clone;
        }
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