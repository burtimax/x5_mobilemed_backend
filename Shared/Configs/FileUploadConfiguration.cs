namespace Shared.Configs;

public class FileUploadConfiguration
{
    public const string Section = "FileUpload";

    /// <summary>
    /// Разрешенные форматы файлов
    /// </summary>
    public List<string> AllowedFormats { get; set; } = new();

    /// <summary>
    /// Путь к папке для сохранения файлов
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Максимальный размер файла в байтах (по умолчанию 100 МБ)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 МБ
}

