using ImGuiNET;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu.Framework
{
    public struct FuGridDefinition
    {
        public int NbColumns { get; private set; }
        public int MinSecondColumnSize { get; private set; }
        public float[] ColumnsWidth { get; private set; }
        public int ColumnWidth { get; private set; }
        public float ResponsiveMinWidth { get; private set; }
        public FuGridType GridType { get; private set; }
        public const float MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE = 196f;

        #region Pressets
        static readonly FuGridDefinition _defaultAutoGrid = new FuGridDefinition(2);
        /// <summary>
        /// Create a default TwoColumns auto-width grid
        /// </summary>
        public static FuGridDefinition DefaultAuto { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultAutoGrid; } }

        static readonly FuGridDefinition _defaultFixedGrid = new FuGridDefinition(2, new int[] { 96 }, 196);
        /// <summary>
        /// Create a default TwoColumns FixedSize grid. The first row is 128px, the second is remaning width. If the second go under 196px, the first will start reduce untill the all goes under min responsive width
        /// </summary>
        public static FuGridDefinition DefaultFixed { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFixedGrid; } }

        static readonly FuGridDefinition _defaultRatioGrid = new FuGridDefinition(2, new float[] { 0.5f }, 196);
        /// <summary>
        /// Create a default TwoColumns FixedSize grid. The first row is 50% of the H space, se second is remaning width. If the second go under 196px, the first will start reduce untill the all goes under min responsive width
        /// </summary>
        public static FuGridDefinition DefaultRatio { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultRatioGrid; } }

        static readonly FuGridDefinition _defaultFlexible = new FuGridDefinition(64f);
        /// <summary>
        /// Create a default Flexible grid. The number of cols will be determinated by the fixed (pixels) columns width and available H width
        /// </summary>
        public static FuGridDefinition DefaultFlexible { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFlexible; } }
        #endregion

        #region Constructors
        /// <summary>
        /// This will create a FixedWidth type Grid.
        /// That mean at least one of the columns has a fixed pixels width.
        /// It use Minimum second column size to determinate whatever the first fixed columns need to be erased
        /// </summary>
        /// <param name="nbCols">Number of columns to create</param>
        /// <param name="colsPixels">array that represent the width of the columns (can be smaller than the number of cols)</param>
        /// <param name="minSecondColumnSize">represent the minimus size of the second columns before resizing the first. Keep -1 for ignore that feature</param>
        /// <param name="responsiveMinWidth">represent the minimus width of the wole grid before responsive layout</param>
        public FuGridDefinition(int nbCols, int[] colsPixels, int minSecondColumnSize = -1, float responsiveMinWidth = -1f)
        {
            NbColumns = nbCols;
            ColumnsWidth = new float[Math.Min(nbCols, colsPixels.Length)];
            for (int i = 0; i < ColumnsWidth.Length; i++)
            {
                ColumnsWidth[i] = (float)colsPixels[i];
            }
            MinSecondColumnSize = minSecondColumnSize;
            ColumnWidth = 0;
            GridType = FuGridType.FixedWidth;
            ResponsiveMinWidth = responsiveMinWidth;
        }

        /// <summary>
        /// This will create a basic auto-width grid
        /// </summary>
        /// <param name="nbCols">number of columns of the grid</param>
        public FuGridDefinition(int nbCols)
        {
            NbColumns = nbCols;
            ColumnsWidth = null;
            MinSecondColumnSize = -1;
            ColumnWidth = 0;
            GridType = FuGridType.Auto;
            ResponsiveMinWidth = -1f;
        }

        /// <summary>
        /// This will create a RationWidth grid
        /// That mean at least one of the columns has a ratio pixels width.
        /// It use Minimum second column size to determinate whatever the first fixed columns need to be erased
        /// </summary>
        /// <param name="nbCols">Number of columns to create</param>
        /// <param name="colsRatios">array that represent the width ratio (percent 0.0f-1.0f of available width) of the columns (can be smaller than the number of cols)</param>
        /// <param name="minSecondColumnSize">represent the minimus size of the second columns before resizing the first. Keep -1 for ignore that feature</param>
        /// <param name="responsiveMinWidth">represent the minimus width of the wole grid before responsive layout</param>
        public FuGridDefinition(int nbCols, float[] colsRatios, int minSecondColumnSize = -1, float responsiveMinWidth = -1f)
        {
            NbColumns = nbCols;
            ColumnsWidth = colsRatios;
            MinSecondColumnSize = minSecondColumnSize;
            ColumnWidth = 0;
            GridType = FuGridType.RatioWidth;
            ResponsiveMinWidth = responsiveMinWidth;
        }

        /// <summary>
        /// This will create a flexible Grid.
        /// That mean that the number of columns will be determinated by the size of a columns and available width
        /// </summary>
        /// <param name="colWidth">fixed width of a columns</param>
        public FuGridDefinition(float colWidth)
        {
            NbColumns = 0;
            ColumnWidth = (int)colWidth;
            MinSecondColumnSize = -1;
            ColumnsWidth = null;
            GridType = FuGridType.FlexibleCols;
            ResponsiveMinWidth = -1f;
        }
        #endregion

        /// <summary>
        /// Setup the current table columns according to this grid definition
        /// </summary>
        /// <param name="gridName">Unique name of the grid</param>
        /// <param name="cellPadding">space between each cells (rows padding)</param>
        /// <param name="outterPadding">padding on left and right out of the grid</param>
        /// <param name="linesBg">colorize evens rows</param>
        /// <param name="isResponsivelyResized">whatever this method determinate if the grid need to be resized (if it's too small). At this point, the grid has beed resized for you</param>
        /// <param name="width">target width of the row (be carefull, you can draw out of current container)</param>
        /// <returns>true if the grid was created</returns>
        internal bool SetupTable(string gridName, float cellPadding, float outterPadding, bool linesBg, ref bool isResponsivelyResized, float width = -1)
        {
            outterPadding *= Fugui.CurrentContext.Scale;
            isResponsivelyResized = false;
            // prepare columns width
            float[] colWidth;
            int nbCols = NbColumns;
            float availWidth = width <= 0f ? ImGui.GetContentRegionAvail().x - (outterPadding * 2f) : width;
            switch (GridType)
            {
                // set auto width
                // we don't set any column width, we let ImGui decide their size
                // can not be forced to responsive
                case FuGridType.Auto:
                    colWidth = null;
                    break;

                // set fixed width
                // at least one of the columns has a fixed pixels width.
                // it use Minimum second column size to determinate whatever the first fixed columns need to be erased
                case FuGridType.FixedWidth:
                    colWidth = new float[ColumnsWidth.Length];
                    float currentRemaningWidth = availWidth;
                    for (int i = 0; i < colWidth.Length; i++)
                    {
                        float targetUnscaledWidth = ColumnsWidth[i];
                        if(targetUnscaledWidth < 0f)
                        {
                            targetUnscaledWidth = currentRemaningWidth - targetUnscaledWidth;
                        }
                        colWidth[i] = targetUnscaledWidth * Fugui.CurrentContext.Scale;
                        currentRemaningWidth -= (colWidth[i] + cellPadding);
                    }
                    if (NbColumns == 2 && MinSecondColumnSize > 0 && ColumnsWidth.Length > 0)
                    {
                        // check if second col width is smaller that min
                        if (availWidth - ColumnsWidth[0] < MinSecondColumnSize)
                        {
                            colWidth = new float[2];
                            colWidth[0] = Math.Max(16f, availWidth - (MinSecondColumnSize * Fugui.CurrentContext.Scale));
                            colWidth[1] = Math.Max(16f, availWidth - colWidth[0]);
                        }
                    }

                    float responsiveMinWidth = ResponsiveMinWidth == -1f ? (MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE * Fugui.CurrentContext.Scale) : (ResponsiveMinWidth * Fugui.CurrentContext.Scale);
                    if (availWidth <= responsiveMinWidth)
                    {
                        nbCols = 1;
                        colWidth = new float[1] { availWidth };
                        isResponsivelyResized = true;
                    }
                    break;

                // prepare ratio width
                // at least one of the columns has a ratio width (between 0 and 1, it's percent of available width).
                // it use Minimum second column size to determinate whatever the first fixed columns need to be erased
                case FuGridType.RatioWidth:
                    colWidth = new float[ColumnsWidth.Length];
                    for (int i = 0; i < colWidth.Length; i++)
                    {
                        colWidth[i] = availWidth * ColumnsWidth[i];
                    }

                    if (NbColumns == 2 && MinSecondColumnSize > 0 && colWidth.Length == 2)
                    {
                        // check if second col width is smaller that min
                        if (availWidth - ColumnsWidth[0] < (MinSecondColumnSize * Fugui.CurrentContext.Scale))
                        {
                            colWidth[0] = Math.Max(16f, availWidth - (MinSecondColumnSize * Fugui.CurrentContext.Scale));
                            colWidth[1] = Math.Max(16f, availWidth - colWidth[0]);
                        }
                    }

                    if (availWidth <= (MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE * Fugui.CurrentContext.Scale))
                    {
                        nbCols = 1;
                        colWidth = new float[1] { availWidth };
                        isResponsivelyResized = true;
                    }
                    break;

                // prepare flexible grid layout
                // this mode derterminate the number of rows, according to a fixed column width
                // can not be forced to responsive
                case FuGridType.FlexibleCols:
                    // get nb of columns
                    float scaledColWidth = ColumnWidth * Fugui.CurrentContext.Scale;
                    nbCols = Mathf.FloorToInt(availWidth / (scaledColWidth + cellPadding * 2f));
                    if (nbCols < 1)
                    {
                        nbCols = 1;
                    }
                    // set nul width array so we will let imgui decide of the size
                    colWidth = null;
                    break;

                default:
                    return false;
            }

            // try to create the table
            ImGuiNative.igSetCursorPosX(ImGuiNative.igGetCursorPosX() + outterPadding);
            bool tableCreated = ImGui.BeginTable(gridName, nbCols, linesBg ? ImGuiTableFlags.RowBg : ImGuiTableFlags.None, new Vector2(availWidth, 0f));
            if (!tableCreated)
            {
                return false;
            }

            // set columns width
            for (int i = 0; i < nbCols; i++)
            {
                // this column width must be forced
                if (colWidth != null && i < colWidth.Length)
                {
                    ImGui.TableSetupColumn(gridName + "col" + i, ImGuiTableColumnFlags.WidthFixed, colWidth[i]);
                }
                // this column width must be auto determinated by ImGui
                else
                {
                    ImGui.TableSetupColumn(gridName + "col" + i);
                }
            }
            return true;
        }
    }
}