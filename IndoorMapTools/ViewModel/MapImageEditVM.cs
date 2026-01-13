using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndoorMapTools.Core;
using IndoorMapTools.Model;
using IndoorMapTools.Services.Presentation;
using System;
using System.Windows.Media.Imaging;

namespace IndoorMapTools.ViewModel
{
    public partial class MapImageEditVM : ObservableObject
    {
        private readonly BackgroundService backgroundWorker;

        public MapImageEditVM(BackgroundService backgroundWorker) => this.backgroundWorker = backgroundWorker;

        [ObservableProperty] private Floor model;
        [ObservableProperty] private BitmapImage replacementImage;
        [ObservableProperty] private int leftPad;
        [ObservableProperty] private int rightPad;
        [ObservableProperty] private int topPad;
        [ObservableProperty] private int bottomPad;

        partial void OnModelChanged(Floor oldModel, Floor newModel)
        {
            //void onCRSChanged(object sender, PropertyChangedEventArgs e)
            //{
            //    CRSName = new EPSGToName().Convert(Model.CRS) as string;
            //    CRSWKT = new EPSGToWKT().Convert(Model.CRS) as string;
            //    bool crsValid = CRSName != null && CRSName.Length > 0;
            //    if(CRSValid != crsValid)
            //    {
            //        CRSValid = crsValid;
            //        IsLayoutValid = crsValid && AreLandmarkOutlinesComplete;
            //    }
            //}

            //if(oldModel != null) oldModel.PropertyChanged -= onCRSChanged;
            //if(newModel != null)
            //{
            //    newModel.PropertyChanged += onCRSChanged;
            //    onCRSChanged(null, null);
            //}
        }

        [RelayCommand] private void InitDialogState(Floor newModel)
        {
            if(Model != newModel) Model = newModel;

            ReplacementImage = null;
            LeftPad = 0;
            RightPad = 0;
            TopPad = 0;
            BottomPad = 0;
        }

        [RelayCommand] private void SetReplacementImage(string filePath)
        {
            if(filePath == null) ReplacementImage = null;
            else
            {
                var image = ImageAlgorithms.BitmapImageFromFile(filePath);
                int curWidth = LeftPad + Model.MapImage.PixelWidth + RightPad;
                int curHeight = TopPad + Model.MapImage.PixelHeight + BottomPad;
                if(image.PixelWidth != curWidth || image.PixelHeight != curHeight)
                {
                    string errText = "이미지의 최종 크기와 대체 이미지의 크기가 일치하지 않습니다." +
                        " 최종 크기와 대체 이미지의 크기가 일치하도록 패딩/클립값을 조절한 후" +
                        " 다시 이미지를 불러와 주시기 바랍니다." +
                        "\n 이미지 최종 크기 : 가로 " + curWidth + " px, 세로" + curHeight + " px" +
                        "\n 대체 이미지 크기 : 가로 " + image.PixelWidth + " px, 세로" + image.PixelHeight + " px";
                    throw new InvalidOperationException(errText);
                }

                ReplacementImage = image;
            }
        }

        [RelayCommand] private void EditMapImage()
        {
            Console.WriteLine($"Padding: {LeftPad}, {RightPad}, {TopPad}, {BottomPad}");
        }
    }
}
