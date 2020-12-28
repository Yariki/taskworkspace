using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace TaskWorkspace.Helpers
{
    public static class UIExtensions
    {
        private static readonly int StandardInputDialogWidth = 355;
        private static readonly int StandardInputDialogHeight = 152;

        internal static Point GetInputWindowCoordinate()
		{
            var focusHandle = NativeHelpers.GetFocus();
            var window = HwndSource.FromHwnd(focusHandle).RootVisual as Window;
            if(window == null)
			{
                return new Point(Screen.PrimaryScreen.WorkingArea.Width / 2 - StandardInputDialogWidth, Screen.PrimaryScreen.WorkingArea.Height / 2 - StandardInputDialogHeight);
			}
            var x = window.Left + window.ActualWidth / 2 - StandardInputDialogWidth;
            var y = window.Top + window.Height / 2 -  StandardInputDialogHeight;

            return new Point(x,y);
		}

        internal static IEnumerable<T> GetChildren<T>(this DependencyObject obj) where T : class
        {
            if (obj == null)
                return default(IEnumerable<T>);
            var list = new List<T>();
            FindChildOfType<T>(obj, list);
            return list;
        }

        private static void FindChildOfType<T>(this DependencyObject obj, List<T> list) where T : class
        {
            var element = GetUIElement(obj);
            if (element == null)
                return;
            if (element is T && !list.Contains(element as T))
            {
                list.Add(element as T);
            }
            var children = ((UIElement)element).GetChildrenProperty();
            if (children != null)
            {
                foreach (var uiElement in children)
                {
                    if (uiElement is T)
                    {
                        list.Add(uiElement as T);
                    }
                    FindChildOfType<T>(uiElement, list);
                }
            }
        }

        private static UIElement GetUIElement(DependencyObject obj)
        {
            if (obj is ContentControl)
            {
                return ((ContentControl)obj).Content as UIElement;
            }
            return (UIElement)obj;
        }

        private static IEnumerable<UIElement> GetChildrenProperty(this UIElement uiElement)
        {
            var property = uiElement.GetType()
                .GetProperty("Children", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property != null)
            {
                return ((UIElementCollection)property.GetValue(uiElement, null)).OfType<UIElement>();
            }
            return uiElement.GetVisualChildren().OfType<UIElement>();
        }

        public static IEnumerable<DependencyObject> GetVisualChildren(this DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int counter = 0; counter < childCount; ++counter)
                yield return VisualTreeHelper.GetChild(parent, counter);
        }

        public static IEnumerable<DependencyObject> GetAllVisualChildren(this DependencyObject parent)
        {
            List<DependencyObject> list1 = new List<DependencyObject>();
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int childIndex = 0; childIndex < childrenCount; ++childIndex)
                list1.Add(VisualTreeHelper.GetChild(parent, childIndex));
            List<DependencyObject> list2 = new List<DependencyObject>();
            foreach (DependencyObject parent1 in list1)
            {
                list2.Add(parent1);
                list2.AddRange(GetAllVisualChildren(parent1));
            }
            return (IEnumerable<DependencyObject>)list2;
        }

    }
}