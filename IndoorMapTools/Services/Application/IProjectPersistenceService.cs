using IndoorMapTools.Model;

namespace IndoorMapTools.Services.Application
{
    public interface IProjectPersistenceService
    {
        public Project LoadProject(string filePath);
        public void SaveProject(Project project, string filePath);
    }
}
