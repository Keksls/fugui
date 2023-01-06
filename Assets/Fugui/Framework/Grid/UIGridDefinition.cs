using ImGuiNET;
using System;
using System.Runtime.CompilerServices;

namespace Fugui.Framework
{
    public struct UIGridDefinition
    {
        public int NbColumns { get; private set; }
        public int MinSecondColumnSize { get; private set; }
        public float[] ColumnsWidth { get; private set; }
        public int ColumnWidth { get; private set; }
        public UIGridType GridType { get; private set; }
        public const float MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE = 196f;

        #region Pressets
        static readonly UIGridDefinition _defaultAutoGrid = new UIGridDefinition(2);
        /// <summary>
        /// Create a default TwoColumns auto-width grid
        /// </summary>
        public static UIGridDefinition DefaultAuto { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultAutoGrid; } }

        static readonly UIGridDefinition _defaultFixedGrid = new UIGridDefinition(2, new int[] { 96 }, 196);
        /// <summary>
        /// Create a default TwoColumns FixedSize grid. The first row is 128px, the second is remaning width. If the second go under 196px, the first will start reduce untill the all goes under min responsive width
        /// </summary>
        public static UIGridDefinition DefaultFixed { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFixedGrid; } }

        static readonly UIGridDefinition _defaultRatioGrid = new UIGridDefinition(2, new float[] { 0.5f }, 196);
        /// <summary>
        /// Create a default TwoColumns FixedSize grid. The first row is 50% of the H space, se second is remaning width. If the second go under 196px, the first will start reduce untill the all goes under min responsive width
        /// </summary>
        public static UIGridDefinition DefaultRatio { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultRatioGrid; } }

        static readonly UIGridDefinition _defaultFlexible = new UIGridDefinition(64);
        /// <summary>
        /// Create a default Flexible grid. The number of cols will be determinated by the fixed (pixels) columns width and available H width
        /// </summary>
        public static UIGridDefinition DefaultFlexible { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _defaultFlexible; } }
        #endregion

        /// <summary>
        /// This will create a FixedWidth type Grid.
        /// That mean at least one of the columns has a fixed pixels width.
        /// It use Minimum second column size to determinate whatever the first fixed columns need to be erased
        /// </summary>
        /// <param name="nbCols">Number of columns to create</param>
        /// <param name="colsPixels">array that represent the width of the columns (can be smaller than the number of cols)</param>
        /// <param name="minSecondColumnSize">represent the minimus size of the second columns before resizing the first. Keep -1 for ignore that feature</param>
        public UIGridDefinition(int nbCols, int[] colsPixels, int minSecondColumnSize = -1)
        {
            NbColumns = nbCols;
            ColumnsWidth = new float[Math.Min(nbCols, colsPixels.Length)];
            for (int i = 0; i < ColumnsWidth.Length; i++)
            {
                ColumnsWidth[i] = (float)colsPixels[i];
            }
            MinSecondColumnSize = minSecondColumnSize;
            ColumnWidth = 0;
            GridType = UIGridType.FixedWidth;
        }

        /// <summary>
        /// This will create a basic auto-width grid
        /// </summary>
        /// <param name="nbCols">number of columns of the grid</param>
        public UIGridDefinition(int nbCols)
        {
            NbColumns = nbCols;
            ColumnsWidth = null;
            MinSecondColumnSize = -1;
            ColumnWidth = 0;
            GridType = UIGridType.Auto;
        }

        /// <summary>
        /// This will create a RationWidth grid
        /// That mean at least one of the columns has a ratio pixels width.
        /// It use Minimum second column size to determinate whatever the first fixed columns need to be erased
        /// </summary>
        /// <param name="nbCols">Number of columns to create</param>
        /// <param name="colsPixels">array that represent the width ratio (percent 0.0f-1.0f of available width) of the columns (can be smaller than the number of cols)</param>
        /// <param name="minSecondColumnSize">represent the minimus size of the second columns before resizing the first. Keep -1 for ignore that feature</param>
        public UIGridDefinition(int nbCols, float[] colsRatios, int minSecondColumnSize = -1)
        {
            NbColumns = nbCols;
            ColumnsWidth = colsRatios;
            MinSecondColumnSize = minSecondColumnSize;
            ColumnWidth = 0;
            GridType = UIGridType.RatioWidth;
        }

        /// <summary>
        /// This will create a flexible Grid.
        /// That mean that the number of columns will be determinated by the size of a columns and available width
        /// </summary>
        /// <param name="colWidth">fixed width of a columns</param>
        public UIGridDefinition(float colWidth)
        {
            NbColumns = 0;
            ColumnWidth = (int)colWidth;
            MinSecondColumnSize = -1;
            ColumnsWidth = null;
            GridType = UIGridType.FlexibleCols;
        }

        /// <summary>
        /// Setup the current table columns according to this grid definition
        /// </summary>
        /// <param name="gridName">Unique nam of the grid</param>
        /// <param name="linesBg">colorize evens rows</param>
        /// <param name="isResponsivelyResized">whatever this method determinate if the grid need to be resized (if it's too small). At this point, the grid has beed resized for you</param>
        /// <returns>true if the grid was created</returns>
        internal bool SetupTable(string gridName, float outterPadding, bool linesBg, ref bool isResponsivelyResized)
        {
            isResponsivelyResized = false;
            // prepare columns width
            float[] colWidth = null;
            int nbCols = NbColumns;
            float availWidth = ImGui.GetContentRegionAvail().x - (outterPadding * 2f);
            switch (GridType)
            {
                // set auto width
                // we don't set any column width, we let ImGui decide their size
                // can not be forced to responsive
                case UIGridType.Auto:
                    colWidth = null;
                    break;

                // set fixed width
                // at least one of the columns has a fixed pixels width.
                // it use Minimum second column size to determinate whatever the first fixed columns need to be erased
                case UIGridType.FixedWidth:
                    colWidth = ColumnsWidth;
                    if (NbColumns == 2 && MinSecondColumnSize > 0 && ColumnsWidth.Length > 0)
                    {
                        // check if second col width is smaller that min
                        if (availWidth - ColumnsWidth[0] < MinSecondColumnSize)
                        {
                            colWidth = new float[2];
                            colWidth[0] = Math.Max(16f, availWidth - MinSecondColumnSize);
                            colWidth[1] = Math.Max(16f, availWidth - colWidth[0]);
                        }
                    }
                    if (availWidth <= MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE)
                    {
                        nbCols = 1;
                        colWidth = new float[1] { availWidth };
                        isResponsivelyResized = true;
                    }
                    break;

                // prepare ratio width
                // at least one of the columns has a ratio width (between 0 and 1, it's percent of available width).
                // it use Minimum second column size to determinate whatever the first fixed columns need to be erased
                case UIGridType.RatioWidth:
                    colWidth = new float[ColumnsWidth.Length];
                    for (int i = 0; i < colWidth.Length; i++)
                    {
                        colWidth[i] = availWidth * ColumnsWidth[i];
                    }

                    if (NbColumns == 2 && MinSecondColumnSize > 0 && colWidth.Length == 2)
                    {
                        // check if second col width is smaller that min
                        if (availWidth - ColumnsWidth[0] < MinSecondColumnSize)
                        {
                            colWidth[0] = Math.Max(16f, availWidth - MinSecondColumnSize);
                            colWidth[1] = Math.Max(16f, availWidth - colWidth[0]);
                        }
                    }

                    if (availWidth <= MINIMUM_GRID_WIDTH_BEFORE_FORCE_RESPONSIVE_RESIZE)
                    {
                        nbCols = 1;
                        colWidth = new float[1] { availWidth };
                        isResponsivelyResized = true;
                    }
                    break;

                // prepare flexible grid layout
                // this mode derterminate the number of rows, according to a fixed column width
                // can not be forced to responsive
                case UIGridType.FlexibleCols:
                    nbCols = (int)(availWidth / ColumnWidth);
                    if (nbCols < 1)
                    {
                        nbCols = 1;
                    }
                    colWidth = new float[nbCols];
                    for (int i = 0; i < nbCols; i++)
                    {
                        colWidth[i] = ColumnWidth;
                    }
                    break;

                default:
                    return false;
            }

            // try to create the table
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + outterPadding);
            bool tableCreated = ImGui.BeginTable(gridName, nbCols, linesBg ? ImGuiTableFlags.RowBg : ImGuiTableFlags.None, new UnityEngine.Vector2(availWidth, 0f));
            if (!tableCreated)
            {
                return false;
            }

            // set columns width
            for (int i = 0; i < nbCols; i++)
            {
                if (colWidth != null && i < colWidth.Length) // this column width must be forced
                {
                    ImGui.TableSetupColumn(gridName + "col" + i, ImGuiTableColumnFlags.WidthFixed, colWidth[i]);
                }
                else // this column width must be auto determinated by ImGui
                {
                    ImGui.TableSetupColumn(gridName + "col" + i);
                }
            }
            return true;
        }
    }
}