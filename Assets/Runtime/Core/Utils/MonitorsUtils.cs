using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Fugui.Core
{
    public static class MonitorsUtils
    {
        public static List<Monitor> Monitors;
        public static MonitorRect GlobalRect;
        public static int MinWidth = 64;
        public static int MinHeight = 64;
        static MonitorsUtils()
        {
            RefreshMonitorsList();
        }

        public static void RefreshMonitorsList()
        {
            // get monitors list
            Monitors = new List<Monitor>();
            MonitorEnumDelegate med = new MonitorEnumDelegate(MonitorEnum);
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, med, IntPtr.Zero);

            // compute global rect
            int minLeft = int.MaxValue;
            int maxBottom = int.MinValue;
            int minTop = int.MaxValue;
            int maxRight = int.MinValue;
            foreach (Monitor monitor in Monitors)
            {
                if (monitor.WorkingArea.left < minLeft)
                    minLeft = monitor.WorkingArea.left;

                if (monitor.WorkingArea.bottom > maxBottom)
                    maxBottom = monitor.WorkingArea.bottom;

                if (monitor.WorkingArea.right > maxRight)
                    maxRight = monitor.WorkingArea.right;

                if (monitor.WorkingArea.top < minTop)
                    minTop = monitor.WorkingArea.top;
            }
            GlobalRect = new MonitorRect()
            {
                left = minLeft,
                right = maxRight,
                top = minTop,
                bottom = maxBottom
            };
        }

        public static Monitor GetClosestMonitor(int x, int y)
        {
            // is too far left
            if (x < GlobalRect.left)
            {
                return Monitors[0];
            }

            // is too far right
            if (x > GlobalRect.right)
            {
                return Monitors[Monitors.Count - 1];
            }

            // is too far up or bottom
            if (y < GlobalRect.top || y > GlobalRect.bottom)
            {
                foreach (Monitor mi in Monitors)
                {
                    if (x >= mi.WorkingArea.left && x <= mi.WorkingArea.right)
                    {
                        return mi;
                    }
                }
            }

            // if rect is inside a monitor
            foreach (Monitor mi in Monitors)
            {
                if (x >= mi.WorkingArea.left && x <= mi.WorkingArea.right && y >= mi.WorkingArea.top && y <= mi.WorkingArea.bottom)
                {
                    return mi;
                }
            }

            // if fail, should not append, return first monitor
            return Monitors[0];
        }

        public static UnityEngine.Vector2Int GetBestPos(int x, int y, int width, int height, int oldX, int oldY)
        {
            // go right
            if (x > oldX)
            {
                Monitor newMonitor = GetCurrentMonitor(x + width, false);
                Monitor oldMonitor = GetCurrentMonitor(oldX + width, false);
                if (oldMonitor.WorkingArea.left < newMonitor.WorkingArea.left)
                {
                    // window is too big to go other monitor
                    if (height > newMonitor.WorkingArea.bottom - newMonitor.WorkingArea.top)
                    {
                        x = oldMonitor.WorkingArea.right - width;
                    }
                    // window has wrong offset
                    else if (y < newMonitor.WorkingArea.top || y + height > newMonitor.WorkingArea.bottom)
                    {
                        x = oldMonitor.WorkingArea.right - width;
                    }
                }
            }

            // go left
            else if (x < oldX)
            {
                Monitor newMonitor = GetCurrentMonitor(x, true);
                Monitor oldMonitor = GetCurrentMonitor(oldX, true);
                if (oldMonitor.WorkingArea.left > newMonitor.WorkingArea.left)
                {
                    // window is too big to go other monitor
                    if (height > newMonitor.WorkingArea.bottom - newMonitor.WorkingArea.top)
                    {
                        x = oldMonitor.WorkingArea.left;
                    }
                    // window has wrong offset
                    else if (y < newMonitor.WorkingArea.top || y + height > newMonitor.WorkingArea.bottom)
                    {
                        x = oldMonitor.WorkingArea.left;
                    }
                }
            }

            MonitorRect rect = GetMaxRect(x, y, width, height);

            // clamp target value to max rect
            x = Math.Clamp(x, rect.left, rect.right);
            y = Math.Clamp(y, rect.top, rect.bottom);

            if (x + width > rect.right)
            {
                x = rect.right - width;
            }
            if (y + height > rect.bottom)
            {
                y = rect.bottom - height;
            }

            return new UnityEngine.Vector2Int(x, y);
        }

        public static UnityEngine.Vector2Int GetBestSize(int x, int y, int width, int height)
        {
            MonitorRect rect = GetMaxRect(x, y, width, height);

            if (x + width > rect.right)
            {
                width = rect.right - x;
            }
            if (width < MinWidth)
            {
                width = MinWidth;
            }
            if (y + height > rect.bottom)
            {
                height = rect.bottom - y;
            }
            if (height < MinHeight)
            {
                height = MinHeight;
            }

            return new UnityEngine.Vector2Int(width, height);
        }

        public static MonitorRect GetMaxRect(int x, int y, int width, int height)
        {
            List<Monitor> involvedMonitors = GetInvolvedMonitors(x, y, width, height);

            if (involvedMonitors.Count == 1)
            {
                return involvedMonitors[0].WorkingArea;
            }
            if (involvedMonitors.Count == 0)
            {
                return Monitors[0].WorkingArea;
            }
            // compute maximum rect
            int left = int.MaxValue;
            int bottom = int.MaxValue;
            int top = int.MinValue;
            int right = int.MinValue;
            foreach (Monitor monitor in involvedMonitors)
            {
                if (monitor.WorkingArea.left < left)
                {
                    left = monitor.WorkingArea.left;
                }

                if (monitor.WorkingArea.bottom > bottom)
                {
                    bottom = monitor.WorkingArea.bottom;
                }

                if (monitor.WorkingArea.right > right)
                {
                    right = monitor.WorkingArea.right;
                }

                if (monitor.WorkingArea.top < top)
                {
                    top = monitor.WorkingArea.top;
                }
            }
            return new MonitorRect()
            {
                left = left,
                right = right,
                top = top,
                bottom = bottom
            };
        }

        public static List<Monitor> GetInvolvedMonitors(int x, int y, int width, int height)
        {
            List<Monitor> monitors = new List<Monitor>();
            HashSet<int> addedMonitors = new HashSet<int>();

            foreach (Monitor monitor in Monitors)
            {
                // left is inside this monitor
                if (x >= monitor.WorkingArea.left && x < monitor.WorkingArea.right)
                {
                    if (!addedMonitors.Contains(monitor.Index))
                    {
                        addedMonitors.Add(monitor.Index);
                        monitors.Add(monitor);
                    }
                    continue; // continue because we don't know where is right
                }

                // right is inside this monitos
                if (x + width > monitor.WorkingArea.left && x + width < monitor.WorkingArea.right)
                {
                    if (!addedMonitors.Contains(monitor.Index))
                    {
                        addedMonitors.Add(monitor.Index);
                        monitors.Add(monitor);
                    }
                    continue; // break because we reach end of rect
                }

                // middle is inside this monitor
                if (x < monitor.WorkingArea.left && x + width > monitor.WorkingArea.right)
                {
                    if (!addedMonitors.Contains(monitor.Index))
                    {
                        addedMonitors.Add(monitor.Index);
                        monitors.Add(monitor);
                    }
                    continue; // continue because we don't know where is right
                }
            }

            return monitors;
        }

        public static Monitor GetCurrentMonitor(int x, bool left)
        {
            if (left)
            {
                foreach (Monitor monitor in Monitors)
                {
                    // left is inside this monitor
                    if (x >= monitor.WorkingArea.left && x < monitor.WorkingArea.right)
                    {
                        return monitor;
                    }
                }
            }
            else
            {
                foreach (Monitor monitor in Monitors)
                {
                    // left is inside this monitor
                    if (x > monitor.WorkingArea.left && x <= monitor.WorkingArea.right)
                    {
                        return monitor;
                    }
                }
            }

            return Monitors[0];
        }

        #region Native PInvoke
        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref MonitorRect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hmon, ref MonitorInfo mi);

        static bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref MonitorRect lprcMonitor, IntPtr dwData)
        {
            MonitorInfo mi = new MonitorInfo();
            mi.Size = (uint)Marshal.SizeOf(mi);
            bool success = GetMonitorInfo(hMonitor, ref mi);
            if (success)
            {
                Monitors.Add(new Monitor(mi, (byte)Monitors.Count));
            }
            return success;
        }
        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public override string ToString()
        {
            return "L:" + left + " R:" + right + " T:" + top + " B:" + bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public uint Size;
        public MonitorRect Monitor;
        public MonitorRect WorkingArea;
        public uint Flags;
    }

    public class Monitor
    {
        public uint Size;
        public MonitorRect FullArea;
        public MonitorRect WorkingArea;
        public uint Flags;
        public byte Index;

        public Monitor(MonitorInfo mi, byte index)
        {
            Size = mi.Size;
            FullArea = mi.Monitor;
            WorkingArea = mi.WorkingArea;
            Flags = mi.Flags;
            Index = index;
        }

        public override string ToString()
        {
            return "Monitor " + Index + " : " + WorkingArea.ToString();
        }
    }

    public enum MonitorWindowState
    {
        None = 0,
        Maximized = 1,
        Minimized = 2,
        Center = 3,
        HalfLeft = 4,
        HalfRight = 5,
        HalfTop = 6,
        HalfBottom = 7
    }
}