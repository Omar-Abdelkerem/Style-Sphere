using Microsoft.AspNetCore.Http;

namespace StyleSphere.Services;

public static class ImageUploadValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    public static (bool IsValid, string? ErrorMessage, string? SafeExtension) ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Uploaded image file is empty.", null);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, "Image size must be less than 5MB.", null);
        }

        if (file.FileName.Contains('\0'))
        {
            return (false, "Invalid filename.", null);
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            return (false, "Only JPG, PNG, GIF, and WebP images are allowed.", null);
        }

        // Read header magic bytes to verify actual file content matches an allowed image signature
        Span<byte> header = stackalloc byte[12];
        int bytesRead;
        using (var stream = file.OpenReadStream())
        {
            bytesRead = stream.Read(header);
        }

        if (bytesRead < 4)
        {
            return (false, "File content is too small to be a valid image.", null);
        }

        bool validMagicBytes = ext switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 &&
                                 header[0] == 0xFF &&
                                 header[1] == 0xD8 &&
                                 header[2] == 0xFF,

            ".png" => bytesRead >= 8 &&
                      header[0] == 0x89 &&
                      header[1] == 0x50 &&
                      header[2] == 0x4E &&
                      header[3] == 0x47 &&
                      header[4] == 0x0D &&
                      header[5] == 0x0A &&
                      header[6] == 0x1A &&
                      header[7] == 0x0A,

            ".gif" => bytesRead >= 6 &&
                      header[0] == (byte)'G' &&
                      header[1] == (byte)'I' &&
                      header[2] == (byte)'F' &&
                      header[3] == (byte)'8' &&
                      (header[4] == (byte)'7' || header[4] == (byte)'9') &&
                      header[5] == (byte)'a',

            ".webp" => bytesRead >= 12 &&
                       header[0] == (byte)'R' &&
                       header[1] == (byte)'I' &&
                       header[2] == (byte)'F' &&
                       header[3] == (byte)'F' &&
                       header[8] == (byte)'W' &&
                       header[9] == (byte)'E' &&
                       header[10] == (byte)'B' &&
                       header[11] == (byte)'P',

            _ => false
        };

        if (!validMagicBytes)
        {
            return (false, "File content does not match a valid image signature (magic bytes check failed).", null);
        }

        string normalizedExt = ext == ".jpeg" ? ".jpg" : ext;
        return (true, null, normalizedExt);
    }

    public static string GenerateSafeFileName(string safeExtension)
    {
        return $"prod-{Guid.NewGuid():N}{safeExtension}";
    }
}
