using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Fugui.Framework
{
    public class UIDockSpaceDefinition
    {
        //The name of the dock space
        public string Name;

        //The unique identifier of the dock space
        public int ID;

        //The proportion of the dock space relative to its parent
        public float Proportion;

        //The orientation of the dock space
        public UIDockSpaceOrientation Orientation;

        //A list of child dock spaces
        [JsonProperty]
        public List<UIDockSpaceDefinition> Children;

        //A lost of binded windowsdefintion
        [JsonProperty]
        public Dictionary<int, string> WindowsDefinition;

        //Constructor that accepts 4 parameters: name, id, proportion and orientation
        public UIDockSpaceDefinition(string name, int id, float proportion, UIDockSpaceOrientation orientation)
        {
            Name = name;
            ID = id;
            Proportion = proportion;
            Orientation = orientation;
            Children = new List<UIDockSpaceDefinition>();
            WindowsDefinition = new Dictionary<int, string>();
        }

        //Constructor that accepts 2 parameters: name and id, with default values for proportion and orientation
        public UIDockSpaceDefinition(string name, int id)
        {
            Name = name;
            ID = id;
            Proportion = 0.5f;
            Orientation = UIDockSpaceOrientation.None;
            Children = new List<UIDockSpaceDefinition>();
            WindowsDefinition = new Dictionary<int, string>();
        }

        //Method that returns the total number of children, including all children of children
        public int GetTotalChildren()
        {
            int count = Children.Count;

            foreach (var child in Children)
            {
                count += child.GetTotalChildren();
            }

            return count;
        }
        
        //Serialization method
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        //Deserialization method
        public UIDockSpaceDefinition Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<UIDockSpaceDefinition>(json);
        }

        /// <summary>
        /// Method that search for a child dock space with the specified name in the current dock space and its children recursively
        /// </summary>
        /// <param name="dockspaceName">The name of the dock space to search for</param>
        /// <returns>The dock space with the specified name, or null if not found</returns>
        internal UIDockSpaceDefinition SearchInChildren(string dockspaceName)
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
    
