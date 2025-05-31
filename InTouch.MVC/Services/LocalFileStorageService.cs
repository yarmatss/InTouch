namespace InTouch.MVC.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public void DeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, path.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public async Task<string> SaveFile(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
        {
            return null!;
        }

        // Create uploads directory if it doesn't exist
        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
        Directory.CreateDirectory(uploadsFolder);

        // Create unique filename
        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Save file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/uploads/{folderName}/{uniqueFileName}";
    }
}
