using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace IndoorMapTools.ViewModel
{
    public partial class FloorState : ObservableObject
    {
        [ObservableProperty] private bool visible = true;
    }
}
