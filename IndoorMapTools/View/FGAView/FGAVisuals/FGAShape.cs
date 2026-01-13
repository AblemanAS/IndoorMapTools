using System;
using System.Windows.Shapes;
using System.Windows;

namespace IndoorMapTools.View.FGAView.FGAVisuals
{
    public abstract class FGAShape : Shape
    {
        public abstract void CacheDefiningGeometry(Func<int, int, int, Rect> mappingFunction);

        private IFGALayoutMapper coordinator;

        protected override Size MeasureOverride(Size constraint) => default;
        protected override Size ArrangeOverride(Size finalSize)
        {
            coordinator = FGALayoutHelper.SearchLayoutMapper(this);
            if(coordinator != null) CacheDefiningGeometry(coordinator.GetItemLayoutRect);
            return base.ArrangeOverride(finalSize);
        }
    }
}
