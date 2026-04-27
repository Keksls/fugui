
namespace Fu
{
        /// <summary>
        /// Represents the Extension Filter data structure.
        /// </summary>
        public struct ExtensionFilter {
            #region State
            public string Name;
            public string[] Extensions;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Extension Filter class.
            /// </summary>
            /// <param name="filterName">The filter Name value.</param>
            /// <param name="filterExtensions">The filter Extensions value.</param>
            public ExtensionFilter(string filterName, params string[] filterExtensions) {
                Name = filterName;
                Extensions = filterExtensions;
            }
            #endregion
        }
}