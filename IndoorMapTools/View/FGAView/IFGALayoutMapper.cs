using System;
using System.Collections.Generic;
using System.Windows;

namespace IndoorMapTools.View.FGAView
{
    public interface IFGALayoutMapper
    {
        public void UpdateReservation(UIElement item, IEnumerable<(int Floor, int Group, int Area)> identifiers);
        public Rect GetItemLayoutRect(int floor, int group, int area);
    }
}
