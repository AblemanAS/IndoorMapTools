
namespace IndoorMapTools.Services.Presentation
{
    public class ResourceStringService : Application.IResourceStringService
    {
        public string Get(string key) => (string)System.Windows.Application.Current.Resources[key];
        public string this[string key] => Get(key);
    }
}
