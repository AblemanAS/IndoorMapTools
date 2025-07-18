using CommunityToolkit.Mvvm.ComponentModel;

namespace IndoorMapTools.ViewModel
{
    public abstract partial class Entity : ObservableObject
    {
        [ObservableProperty] protected string name;
        public override string ToString() => Name;
    }
}
