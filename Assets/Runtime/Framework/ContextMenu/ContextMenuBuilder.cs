using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public static class ContextMenuItemBuilder
    {
        private static List<ContextMenuItem> _items = new List<ContextMenuItem>();
        private static List<ContextMenuItem> _currentLevel = _items;

        public static void AddItem(string label, string shortcut, bool enabled, Action clickAction)
        {
            _currentLevel.Add(new ContextMenuItem(label, shortcut, enabled, false, clickAction));
        }

        public static void AddItem(string label, bool enabled)
        {
            _currentLevel.Add(new ContextMenuItem(label, string.Empty, enabled, false, null));
        }

        public static void AddItem(string label)
        {
            _currentLevel.Add(new ContextMenuItem(label, string.Empty, true, false, null));
        }

        public static void AddItem(string label, string shortcut, Action clickAction)
        {
            _currentLevel.Add(new ContextMenuItem(label, shortcut, true, false, clickAction));
        }

        public static void AddItem(string label, bool enabled, Action clickAction)
        {
            _currentLevel.Add(new ContextMenuItem(label, string.Empty, enabled, false, clickAction));
        }

        public static void AddItem(string label, Action clickAction)
        {
            _currentLevel.Add(new ContextMenuItem(label, string.Empty, true, false, clickAction));
        }

        public static void AddSeparator()
        {
            _currentLevel.Add(new ContextMenuItem(null, null, false, true, null));
        }

        public static void BeginChild(string label)
        {
            BeginChild(label, true);
        }
        public static void BeginChild(string label, bool enabled)
        {
            var item = new ContextMenuItem(label, string.Empty, enabled, false, null, new List<ContextMenuItem>());
            _currentLevel.Add(item);
            _currentLevel = item.Children;
        }

        public static void EndChild()
        {
            var parentLevel = _items;
            foreach (var item in _items)
            {
                if (item.Children == _currentLevel)
                {
                    _currentLevel = parentLevel;
                    break;
                }
                parentLevel = item.Children;
            }
        }

        public static List<ContextMenuItem> Build()
        {
            List<ContextMenuItem> builded = new List<ContextMenuItem>(_items);
            _items.Clear();
            _currentLevel = _items;
            return builded;
        }
    }
}