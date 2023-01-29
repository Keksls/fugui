using System;
using System.Collections.Generic;
using System.IO;
using Fu.Core;
using Newtonsoft.Json;

namespace Fu.Framework
{
    public class FuDockSpaceDefinition
    {
        //The name of the dock space
        public string Name;

        //The unique identifier of the dock space
        public uint ID;

        //The proportion of the dock space relative to its parent
        public float Proportion;

        //The orientation of the dock space
        public UIDockSpaceOrientation Orientation;

        //A list of child dock spaces
        [JsonProperty]
        public List<FuDockSpaceDefinition> Children;

        //A lost of binded windowsdefintion
        [JsonProperty]
        public Dictionary<int, string> WindowsDefinition;

        public FuDockSpaceDefinition()
        {

        }

        //Constructor that accepts 4 parameters: name, id, proportion and orientation
        public FuDockSpaceDefinition(string name, uint id, float proportion, UIDockSpaceOrientation orientation)
        {
            Name = name;
            ID = id;
            Proportion = proportion;
            Orientation = orientation;
            Children = new List<FuDockSpaceDefinition>();
            WindowsDefinition = new Dictionary<int, string>();
        }

        //Constructor that accepts 2 parameters: name and id, with default values for proportion and orientation
        public FuDockSpaceDefinition(string name, uint id)
        {
            Name = name;
            ID = id;
            Proportion = 0.5f;
            Orientation = UIDockSpaceOrientation.None;
            Children = new List<FuDockSpaceDefinition>();
            WindowsDefinition = new Dictionary<int, string>();
        }

        //Method that returns the total number of children, including all children of children
        public uint GetTotalChildren()
        {
            uint count = (uint) Children.Count;

            foreach (var child in Children)
            {
                count += child.GetTotalChildren();
            }

            return count;
        }
        
        //Serialization method
        public static string Serialize(FuDockSpaceDefinition dockspaceDefinition)
        {
            return JsonConvert.SerializeObject(dockspaceDefinition);
        }

        //Deserialization method
        public static FuDockSpaceDefinition ReadFromFile(string pathFile)
        {
            FuDockSpaceDefinition result = null;

            try
            {
                using (StreamReader sr = new StreamReader(pathFile))
                {
                    result = JsonConvert.DeserializeObject<FuDockSpaceDefinition>(sr.ReadToEnd());
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
        internal FuDockSpaceDefinition SearchInChildren(string dockspaceName)
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
        internal FuDockSpaceDefinition SearchInChildren(int windowDefID)
        {
            if (WindowsDefinition.ContainsKey(windowDefID))
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
        internal void RemoveWindowsDefinitionInChildren(int windowDefID)
        {
            WindowsDefinition.Remove(windowDefID);

            foreach (var child in Children)
            {
                child.RemoveWindowsDefinitionInChildren(windowDefID);
            }
        }

        internal List<FuWindowsNames> GetAllWindowsDefinitions()
        {
            List<FuWindowsNames> windows = new List<FuWindowsNames>();

            foreach (var window in WindowsDefinition)
            {
                windows.Add((FuWindowsNames) window.Key);
            }

            foreach (var child in Children)
            {
                windows.AddRange(child.GetAllWindowsDefinitions());
            }

            return windows;

        }
    }

    //Enum for setting the orientation of a dock space
    public enum UIDockSpaceOrientation
    {
        //None orientation
        None,
        //Horizontal orientation
        Horizontal,
        //Vertical orientation
        Vertical
    }
}
   