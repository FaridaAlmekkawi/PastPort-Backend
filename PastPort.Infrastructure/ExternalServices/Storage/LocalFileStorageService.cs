using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PastPort.Application.Interfaces;
using System.Linq;


namespace PastPort.Infrastructure.ExternalServices.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

      
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        try
        {
           
            var folderPath = Path.Combine(_uploadPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

          
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

          
            var fileUrl = $"/uploads/{folder}/{fileName}";
            _logger.LogInformation("File uploaded successfully: {FileUrl}", fileUrl);

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            throw new Exception("File upload failed", ex);
        }
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
       
            var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
            var filePath = Path.Combine(_uploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public async Task<byte[]> GetFileAsync(string fileUrl)
    {
        try
        {
            var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
            var filePath = Path.Combine(_uploadPath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found");

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file: {FileUrl}", fileUrl);
            throw;
        }
    }

    public bool ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes)
    {
        if (file == null || file.Length == 0)
            return false;

        
        if (file.Length > maxSizeInBytes)
        {
            _logger.LogWarning("File size exceeds limit: {FileSize} bytes", file.Length);
            return false;
        }

     
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("File extension not allowed: {Extension}", extension);
            return false;
        }

        return true;
    }
}