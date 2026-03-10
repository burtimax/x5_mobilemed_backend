/// <summary>
/// Класс для представления пагинированного списка элементов.
/// Содержит информацию о текущей странице, общем количестве страниц и элементов.
/// </summary>
/// <typeparam name="T">Тип элементов в списке</typeparam>

using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Infrastructure.Models;

public class PagedList<T>
{
    /// <summary>
    /// Номер текущей страницы
    /// </summary>
    public int CurrentPage { get; private set; }

    /// <summary>
    /// Общее количество страниц
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Размер страницы (количество элементов на странице)
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Флаг наличия предыдущей страницы
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Флаг наличия следующей страницы
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// Список элементов текущей страницы
    /// </summary>
    public List<T> Data { get; set; } = new List<T>();

    /// <summary>
    /// Создает пустой экземпляр пагинированного списка
    /// </summary>
    public PagedList() { }

    /// <summary>
    /// Создает экземпляр пагинированного списка с указанными параметрами
    /// </summary>
    /// <param name="items">Список элементов</param>
    /// <param name="count">Общее количество элементов</param>
    /// <param name="pageNumber">Номер текущей страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    public PagedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Data.AddRange(items);
    }

    /// <summary>
    /// Создает экземпляр пагинированного списка на основе параметров пагинации
    /// </summary>
    /// <param name="items">Список элементов</param>
    /// <param name="count">Общее количество элементов</param>
    /// <param name="pagination">Параметры пагинации</param>
    public PagedList(List<T> items, int count, Pagination pagination) : this(items, count, pagination.PageNumber, pagination.PageSize)
    {
    }

    /// <summary>
    /// Асинхронно создает пагинированный список из IQueryable
    /// </summary>
    /// <param name="source">Источник данных</param>
    /// <param name="pageNumber">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <returns>Пагинированный список</returns>
    public async static Task<PagedList<T>> ToPagedListAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }

    /// <summary>
    /// Асинхронно создает пагинированный список из IQueryable с использованием параметров пагинации
    /// </summary>
    /// <param name="source">Источник данных</param>
    /// <param name="pagination">Параметры пагинации</param>
    /// <returns>Пагинированный список</returns>
    public static Task<PagedList<T>> ToPagedListAsync(IQueryable<T> source, Pagination pagination)
    {
        return ToPagedListAsync(source, pagination.PageNumber, pagination.PageSize);
    }
}
