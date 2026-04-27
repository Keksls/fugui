using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Class that represent all DrawList for a frame
        /// </summary>
        public class DrawData
        {
            #region State
            public List<DrawList> DrawLists;
            public int TotalVtxCount;
            public int TotalIdxCount;
            public Vector2 DisplayPos;
            public Vector2 DisplaySize;
            public Vector2 FramebufferScale;
            public int CmdListsCount;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Draw Data class.
            /// </summary>
            public DrawData()
            {
                DrawLists = new List<DrawList>();
                Clear();
            }
            #endregion

            #region Methods
            /// <summary>
            /// Clear all Draw Lists
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i < DrawLists.Count; i++)
                {
                    DrawLists[i].Dispose();
                }
                DrawLists.Clear();
                TotalVtxCount = 0;
                TotalIdxCount = 0;
                CmdListsCount = 0;
            }

            /// <summary>
            /// Add some Draw Lists
            /// </summary>
            /// <param name="dLists">Draw Lists to Add</param>
            public void AddDrawLists(IEnumerable<DrawList> dLists)
            {
                foreach (DrawList drawList in dLists)
                {
                    AddDrawList(drawList);
                }
            }

            /// <summary>
            /// Add a DrawList
            /// </summary>
            /// <param name="dList">DrawList to Add</param>
            public void AddDrawList(DrawList dList)
            {
                DrawLists.Add(dList);
                CmdListsCount++;
                TotalVtxCount += dList.VtxBuffer.Length;
                TotalIdxCount += dList.IdxBuffer.Length;
            }

            /// <summary>
            /// Bind this drawData from ImGui drawData Ptr
            /// </summary>
            /// <param name="imDrawData">ImDrawDataPtr for this frame</param>
            public void Bind(ImDrawDataPtr imDrawData)
            {
                Clear();
                for (int i = 0; i < imDrawData.CmdListsCount; i++)
                {
                    AddDrawList(new DrawList(imDrawData.CmdLists[i]));
                }
                FramebufferScale = imDrawData.FramebufferScale;
                DisplayPos = imDrawData.DisplayPos;
                DisplaySize = imDrawData.DisplaySize;
            }
            #endregion
        }
}