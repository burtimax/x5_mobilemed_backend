namespace Api;

/// <summary>
/// Базовый класс ответа API с типизированными данными
/// </summary>
/// <typeparam name="T">Тип данных ответа</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// Данные ответа
    /// </summary>
    public T Data { get; set; }

    /// <summary>
    /// Описание ответа или сообщение об ошибке
    /// </summary>
    public string? Description { get; set; }
}