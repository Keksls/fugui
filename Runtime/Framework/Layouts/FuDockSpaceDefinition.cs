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
        public float Proportion = 0.5f;
        /// <summary>
        /// The orientation of the dock space
        /// </summary>
        public UIDockSpaceOrientation Orientation = UIDockSpaceOrientation.None;
        /// <summary>
        /// A list of child dock spaces
        /// </summary>
        [JsonProperty]
        public List<FuDockingLayoutDefinition> Children = new List<FuDockingLayoutDefinition>();
        /// <summary>
        /// A list of binded windowsdefintion
        /// </summary>
        [JsonProperty]
        public List<ushort> WindowsDefinition = new List<ushort>();
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
            if (dockspaceDefinition == null)
            {
                throw new ArgumentNullException(nameof(dockspaceDefinition));
            }
            if (!dockspaceDefinition.TryValidate(out string validationError))
            {
                throw new InvalidOperationException($"Cannot serialize an invalid docking layout: {validationError}");
            }

            return JsonConvert.SerializeObject(dockspaceDefinition);
        }

        /// <summary>
        /// Create a deep copy that can be safely mutated at runtime.
        /// </summary>
        public FuDockingLayoutDefinition Clone()
        {
            if (!TryValidate(out string validationError))
            {
                throw new InvalidOperationException($"Cannot clone an invalid docking layout: {validationError}");
            }

            return CloneNode(this);
        }

        /// <summary>
        /// Creates a recursive copy after the complete source tree has been validated.
        /// </summary>
        /// <param name="source">Validated source node to copy.</param>
        /// <returns>Independent copy of the source node tree.</returns>
        private static FuDockingLayoutDefinition CloneNode(FuDockingLayoutDefinition source)
        {
            // Validation guarantees that recursive traversal cannot encounter nulls, sharing or cycles.
            FuDockingLayoutDefinition clone = new FuDockingLayoutDefinition(source.Name, source.ID, source.Proportion, source.Orientation)
            {
                AutoHideTopBar = source.AutoHideTopBar,
                LayoutType = source.LayoutType,
                WindowsDefinition = new List<ushort>(source.WindowsDefinition),
                Children = new List<FuDockingLayoutDefinition>()
            };

            foreach (FuDockingLayoutDefinition child in source.Children)
            {
                clone.Children.Add(CloneNode(child));
            }

            return clone;
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
                if (result != null && !result.TryValidate(out string validationError))
                {
                    UnityEngine.Debug.LogWarning($"Invalid Fugui docking layout '{pathFile}': {validationError}");
                    result = null;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(ex.GetBaseException().Message);
            }

            return result;
        }

        /// <summary>
        /// Validates the complete docking tree without mutating it.
        /// </summary>
        /// <param name="validationError">Description of the first invalid invariant.</param>
        /// <returns>True when the layout can be cloned and applied safely.</returns>
        public bool TryValidate(out string validationError)
        {
            // Reference tracking rejects both direct cycles and shared subtrees.
            HashSet<FuDockingLayoutDefinition> visitedNodes = new HashSet<FuDockingLayoutDefinition>();
            return TryValidateNode(this, "root", visitedNodes, out validationError);
        }

        /// <summary>
        /// Validates one layout node and all of its descendants.
        /// </summary>
        /// <param name="node">Node currently being validated.</param>
        /// <param name="path">Readable path used in validation errors.</param>
        /// <param name="visitedNodes">Node references already visited in this tree.</param>
        /// <param name="validationError">Description of the first invalid invariant.</param>
        /// <returns>True when this complete subtree is structurally valid.</returns>
        private static bool TryValidateNode(
            FuDockingLayoutDefinition node,
            string path,
            HashSet<FuDockingLayoutDefinition> visitedNodes,
            out string validationError)
        {
            // Validate local shape before descending so consumers never see partially trusted data.
            if (node == null)
            {
                validationError = $"Node '{path}' is null.";
                return false;
            }
            if (!visitedNodes.Add(node))
            {
                validationError = $"Node '{path}' is referenced more than once or creates a cycle.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(node.Name))
            {
                validationError = $"Node '{path}' has no name.";
                return false;
            }
            if (node.Children == null)
            {
                validationError = $"Node '{path}' has a null children collection.";
                return false;
            }
            if (node.WindowsDefinition == null)
            {
                validationError = $"Node '{path}' has a null window collection.";
                return false;
            }
            if (float.IsNaN(node.Proportion) ||
                float.IsInfinity(node.Proportion) ||
                node.Proportion < 0.05f ||
                node.Proportion > 0.95f)
            {
                validationError = $"Node '{path}' has an invalid split proportion ({node.Proportion}).";
                return false;
            }
            if (!Enum.IsDefined(typeof(UIDockSpaceOrientation), node.Orientation))
            {
                validationError = $"Node '{path}' has an invalid orientation ({node.Orientation}).";
                return false;
            }

            bool isLeaf = node.Children.Count == 0;
            if (isLeaf && node.Orientation != UIDockSpaceOrientation.None)
            {
                validationError = $"Leaf node '{path}' must use the None orientation.";
                return false;
            }
            if (!isLeaf &&
                (node.Children.Count != 2 || node.Orientation == UIDockSpaceOrientation.None))
            {
                validationError = $"Split node '{path}' must have exactly two children and a split orientation.";
                return false;
            }
            if (!isLeaf && node.WindowsDefinition.Count > 0)
            {
                validationError = $"Split node '{path}' cannot own windows.";
                return false;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                string childPath = $"{path}/{node.Children[i]?.Name ?? i.ToString()}";
                if (!TryValidateNode(node.Children[i], childPath, visitedNodes, out validationError))
                {
                    return false;
                }
            }

            validationError = null;
            return true;
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
