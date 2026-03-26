using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

/// <summary>Запись о попытке запроса к LLM (схема stat).</summary>
public class LlmUsageEntity : BaseEntity
{
    [Comment("Входящий запрос к LLM в формате JSON")]
    public string InputJson { get; set; } = "";

    [Comment("Длительность HTTP-запроса, мс")]
    public long DurationMs { get; set; }

    [Comment("Успешное завершение попытки (HTTP и разбор ответа)")]
    public bool IsSuccess { get; set; }

    [Comment("Текст ответа ассистента или сырое тело ответа при сбое разбора")]
    public string? LlmResponse { get; set; }

    [Comment("Сообщение об ошибке при неуспехе")]
    public string? ErrorMessage { get; set; }

    [Comment("Число токенов на входе")]
    public int? PromptTokens { get; set; }

    [Comment("Число токенов на выходе")]
    public int? CompletionTokens { get; set; }

    [Comment("Имя модели LLM")]
    [MaxLength(512)]
    public string? LlmModel { get; set; }

    [Comment("Стоимость запроса (если провайдер вернул)")]
    public decimal? Cost { get; set; }

    [Comment("Идентификатор запроса/ответа у провайдера LLM")]
    [MaxLength(128)]
    public string? LlmRequestId { get; set; }
}
