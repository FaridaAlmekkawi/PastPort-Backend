using Microsoft.AspNetCore.Http;

namespace PastPort.Application.Interfaces;

public interface IFileStorageService
{
    
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<byte[]> GetFileAsync(string fileUrl);
    bool ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes);
    Task<bool> DeleteFolderAsync(string folder);
    long GetFileSize(string fileUrl);
    bool FileExists(string fileUrl);
}