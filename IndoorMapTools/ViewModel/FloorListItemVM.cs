using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Application;
using IndoorMapTools.Services.Domain;
using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.ViewModel
{
    public partial class FloorListItemVM : ObservableObject
    {
        private readonly FloorMapEditService fmeSvc;
        private readonly IMessageService msgSvc;
        private readonly IResourceStringService strSvc;

        [ObservableProperty] private Floor model;
        [ObservableProperty] private int replicationCount = 1;
        [ObservableProperty] private int leftPad;
        [ObservableProperty] private int rightPad;
        [ObservableProperty] private int topPad;
        [ObservableProperty] private int bottomPad;
        [ObservableProperty] private int newWidth;
        [ObservableProperty] private int newHeight;
        [ObservableProperty] private BitmapImage replacementImage;


        public FloorListItemVM(FloorMapEditService fmeSvc, IMessageService msgSvc, IResourceStringService strSvc)
        {
            this.fmeSvc = fmeSvc;
            this.msgSvc = msgSvc;
            this.strSvc = strSvc;
        }


        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.PropertyName == nameof(LeftPad) || e.PropertyName == nameof(RightPad))
                NewWidth = Model.MapImage.PixelWidth + LeftPad + RightPad;
            if(e.PropertyName == nameof(TopPad) || e.PropertyName == nameof(BottomPad))
                NewHeight = Model.MapImage.PixelHeight + TopPad + BottomPad;
        }


        [RelayCommand] private void InitDialogState(Floor newModel)
        {
            if(Model != newModel) Model = newModel;

            LeftPad = 0;
            RightPad = 0;
            TopPad = 0;
            BottomPad = 0;
            NewWidth = Model.MapImage.PixelWidth;
            NewHeight = Model.MapImage.PixelHeight;
            ReplacementImage = null;
        }

        [RelayCommand] private void ReorderFloor(int destIndex) => Model.ParentBuilding.MoveFloor(Model, destIndex);
        [RelayCommand] private void ReplicateFloor() => EntityOrganizer.ReplicateFloor(Model, ReplicationCount);
        [RelayCommand] private void PadCropMapImage() => fmeSvc.PadCropMapImage(Model, LeftPad, RightPad, TopPad, BottomPad);

        [RelayCommand] private void LoadReplacementImage(string[] filePaths)
        {
            BitmapImage loadedImage;
            try { loadedImage = ImageAlgorithms.BitmapImageFromFile(filePaths[0]); } catch { return; }
            if(loadedImage == null) return;

            if(Model.MapImage.PixelHeight != loadedImage.PixelHeight || Model.MapImage.PixelWidth != loadedImage.PixelWidth)
                msgSvc.ShowError(strSvc["strings.ReplaceMapImageSizeUnmatchErrorMessage"] + 
                    "\n" + strSvc["strings.ImageDimension"] + " (px) : " + 
                    $"{Model.MapImage.PixelWidth}×{Model.MapImage.PixelHeight} " +
                    $"→ {loadedImage.PixelWidth}×{loadedImage.PixelHeight}");
            else  ReplacementImage = loadedImage;
        }

        [RelayCommand] private void ReplaceMapImage()
        { if(ReplacementImage != null) Model.MapImage = ReplacementImage; }
    }
}
