using System.Collections.Generic;
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

        //Constructor that accepts 4 parameters: name, id, proportion and orientation
        public UIDockSpaceDefinition(string name, int id, float proportion, UIDockSpaceOrientation orientation)
        {
            Name = name;
            ID = id;
            Proportion = proportion;
            Orientation = orientation;
            Children = new List<UIDockSpaceDefinition>();
        }

        //Constructor that accepts 2 parameters: name and id, with default values for proportion and orientation
        public UIDockSpaceDefinition(string name, int id)
        {
            Name = name;
            ID = id;
            Proportion = 0.5f;
            Orientation = UIDockSpaceOrientation.None;
            Children = new List<UIDockSpaceDefinition>();
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
        public static UIDockSpaceDefinition Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<UIDockSpaceDefinition>(json);
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
    
