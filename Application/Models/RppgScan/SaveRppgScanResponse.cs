using Infrastructure.Db.App.Entities;

namespace Application.Models.RppgScan;

public class SaveRppgSсanResponse
{
    /// <summary>
    /// Сущность результата сканирования
    /// </summary>
    public UserRppgScanEntity Scan { get; set; }

    /// <summary>
    /// Расшифровка результатов сканирования
    /// </summary>
    public List<ScanTranscriptItem> Transcripts { get; set; }
}
