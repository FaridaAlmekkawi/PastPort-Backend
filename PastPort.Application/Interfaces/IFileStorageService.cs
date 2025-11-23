using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PastPort.Application.Interfaces
{
   public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<byte[]> GetFileAsync(string fileUrl);
        bool ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes);
    }
}
