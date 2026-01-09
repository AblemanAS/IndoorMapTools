
namespace IndoorMapTools.Services.Application
{
    public interface IMessageService
    {
        void ShowError(string message, string title = null);
        void ShowInfo(string message, string title = null);
        bool Confirm(string message, string title = null);
    }
}
