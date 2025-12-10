using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PastPort.Application.Interfaces;

namespace PastPort.Infrastructure.ExternalServices.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _uploadPath;

    // حدود الملفات
    private const long MaxTotalUploadSize = 1000 * 1024 * 1024; // 1GB

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _uploadPath = Path.Combine(_environment.WebRootPath, "uploads");

        // إنشاء مجلد uploads إذا لم يكن موجوداً
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation("Created uploads directory: {UploadPath}", _uploadPath);
        }
    }

    /// <summary>
    /// رفع ملف بأمان
    /// </summary>
    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        try
        {
            // 1. التحقق من صحة الملف
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("الملف فارغ");
            }

            // 2. إنشاء مجلد فرعي
            var folderPath = Path.Combine(_uploadPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 3. إنشاء اسم ملف فريد وآمن
            var fileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            // 4. التحقق من أن المسار داخل مجلد uploads (حماية من Path Traversal)
            var fullPath = Path.GetFullPath(filePath);
            var fullUploadPath = Path.GetFullPath(_uploadPath);

            if (!fullPath.StartsWith(fullUploadPath))
            {
                throw new InvalidOperationException("محاولة رفع ملف خارج المسار المسموح");
            }

            // 5. حفظ الملف
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6. إنشاء URL نسبي
            var fileUrl = $"/uploads/{folder}/{fileName}";

            _logger.LogInformation(
                "تم رفع الملف بنجاح: {FileName}, الحجم: {FileSize} bytes",
                fileName,
                file.Length);

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في رفع الملف: {FileName}", file.FileName);
            throw new Exception("فشل رفع الملف", ex);
        }
    }

    /// <summary>
    /// حذف ملف بأمان
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                    return false;

                // تحويل URL نسبي إلى مسار فعلي
                var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
                var filePath = Path.Combine(_uploadPath, fileName);

                // التحقق من الأمان
                var fullPath = Path.GetFullPath(filePath);
                var fullUploadPath = Path.GetFullPath(_uploadPath);

                if (!fullPath.StartsWith(fullUploadPath))
                {
                    _logger.LogWarning("محاولة حذف ملف خارج المسار المسموح: {FilePath}", filePath);
                    return false;
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("تم حذف الملف: {FileUrl}", fileUrl);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الملف: {FileUrl}", fileUrl);
                return false;
            }
        });
    }

    /// <summary>
    /// الحصول على محتوى الملف
    /// </summary>
    public async Task<byte[]> GetFileAsync(string fileUrl)
    {
        try
        {
            var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
            var filePath = Path.Combine(_uploadPath, fileName);

            // التحقق من الأمان
            var fullPath = Path.GetFullPath(filePath);
            var fullUploadPath = Path.GetFullPath(_uploadPath);

            if (!fullPath.StartsWith(fullUploadPath))
            {
                throw new InvalidOperationException("محاولة الوصول إلى ملف خارج المسار المسموح");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"الملف غير موجود: {fileUrl}");
            }

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في الحصول على الملف: {FileUrl}", fileUrl);
            throw;
        }
    }

    /// <summary>
    /// التحقق من صحة الملف
    /// </summary>
    public bool ValidateFile(IFormFile file, string[] allowedExtensions, long maxSizeInBytes)
    {
        try
        {
            // 1. التحقق من وجود الملف
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("محاولة رفع ملف فارغ");
                return false;
            }

            // 2. التحقق من الحجم
            if (file.Length > maxSizeInBytes)
            {
                _logger.LogWarning(
                    "حجم الملف يتجاوز الحد المسموح: {FileSize} > {MaxSize}",
                    file.Length,
                    maxSizeInBytes);
                return false;
            }

            // 3. التحقق من امتداد الملف
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("امتداد ملف غير مسموح: {Extension}", extension);
                return false;
            }

            // 4. التحقق من Content Type (إضافي)
            if (!IsValidContentType(file.ContentType, extension))
            {
                _logger.LogWarning(
                    "نوع محتوى غير متطابق: {ContentType} للامتداد {Extension}",
                    file.ContentType,
                    extension);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في التحقق من الملف");
            return false;
        }
    }

    /// <summary>
    /// حذف مجلد كامل
    /// </summary>
    public async Task<bool> DeleteFolderAsync(string folder)
    {
        return await Task.Run(() =>
        {
            try
            {
                var folderPath = Path.Combine(_uploadPath, folder);

                // التحقق من الأمان
                var fullPath = Path.GetFullPath(folderPath);
                var fullUploadPath = Path.GetFullPath(_uploadPath);

                if (!fullPath.StartsWith(fullUploadPath))
                {
                    return false;
                }

                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    _logger.LogInformation("تم حذف المجلد: {Folder}", folder);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف المجلد: {Folder}", folder);
                return false;
            }
        });
    }

    /// <summary>
    /// الحصول على حجم الملف
    /// </summary>
    public long GetFileSize(string fileUrl)
    {
        try
        {
            var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
            var filePath = Path.Combine(_uploadPath, fileName);

            if (!File.Exists(filePath))
                return 0;

            return new FileInfo(filePath).Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في الحصول على حجم الملف");
            return 0;
        }
    }

    /// <summary>
    /// التحقق من وجود الملف
    /// </summary>
    public bool FileExists(string fileUrl)
    {
        try
        {
            var fileName = fileUrl.Replace("/uploads/", "").Replace("/", Path.DirectorySeparatorChar.ToString());
            var filePath = Path.Combine(_uploadPath, fileName);

            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// إنشاء اسم ملف فريد وآمن
    /// </summary>
    private string GenerateUniqueFileName(string originalFileName)
    {
        var fileExtension = Path.GetExtension(originalFileName);
        var fileName = Path.GetFileNameWithoutExtension(originalFileName);

        // إزالة الأحرف الخطرة من الاسم
        var safeName = System.Text.RegularExpressions.Regex.Replace(
            fileName,
            @"[^a-zA-Z0-9_-]",
            "_");

        // إنشاء اسم فريد باستخدام GUID
        return $"{safeName}_{Guid.NewGuid().ToString()[..8]}{fileExtension}";
    }

    /// <summary>
    /// التحقق من توافق Content Type مع الامتداد
    /// </summary>
    private bool IsValidContentType(string? contentType, string extension)
    {
        var validTypes = new Dictionary<string, string[]>
        {
            { ".jpg", new[] { "image/jpeg", "image/jpg" } },
            { ".jpeg", new[] { "image/jpeg", "image/jpg" } },
            { ".png", new[] { "image/png" } },
            { ".gif", new[] { "image/gif" } },
            { ".webp", new[] { "image/webp" } },
            { ".mp3", new[] { "audio/mpeg", "audio/mp3" } },
            { ".wav", new[] { "audio/wav", "audio/x-wav" } },
            { ".ogg", new[] { "audio/ogg" } },
            { ".glb", new[] { "model/gltf-binary", "application/octet-stream" } },
            { ".gltf", new[] { "model/gltf+json", "application/json" } },
            { ".obj", new[] { "model/obj", "application/octet-stream" } },
            { ".fbx", new[] { "application/octet-stream" } },
            { ".pdf", new[] { "application/pdf" } },
        };

        if (!validTypes.ContainsKey(extension))
            return true; // للملفات الجديدة

        if (string.IsNullOrEmpty(contentType))
            return false;

        return validTypes[extension].Contains(contentType);
    }
}