using CommunityToolkit.Mvvm.ComponentModel;

namespace IndoorMapTools.Model
{
    public abstract partial class Entity : ObservableObject
    {
        [ObservableProperty] protected string name;
        public override string ToString() => Name;
        public abstract void Delete();
    }
}
