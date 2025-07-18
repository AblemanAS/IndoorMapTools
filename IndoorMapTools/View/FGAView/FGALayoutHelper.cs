using System;
using System.Windows;
using System.Windows.Media;

namespace IndoorMapTools.View.FGAView
{
    public static class FGALayoutHelper
    {
        public static IFGALayoutMapper SearchLayoutMapper(DependencyObject self)
        {
            for(DependencyObject curObj = self; curObj != null; curObj = VisualTreeHelper.GetParent(curObj))
                if(curObj is IFGALayoutMapper coordinator) return coordinator;
            return null;
        }
    }
}
