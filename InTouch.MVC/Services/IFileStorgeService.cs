namespace InTouch.MVC.Services;

public interface IFileStorageService
{
    Task<string> SaveFile(IFormFile file, string folderName);
    void DeleteFile(string path);
}
