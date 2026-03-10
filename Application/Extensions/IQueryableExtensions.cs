using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Application.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<TSource> WhereIf<TSource>(
            this IQueryable<TSource> source,
            bool condition,
            Expression<Func<TSource, bool>> predicate)
        {
            if (condition)
                return source.Where(predicate);
            else
                return source;
        }
    
        public static IQueryable<T> When<T>(this IQueryable<T> query, bool condition,
            Func<IQueryable<T>, IQueryable<T>> whenTrue, 
            Func<IQueryable<T>, IQueryable<T>>? whenFalse = null)
        {
            if (condition)
            {
                query = whenTrue.Invoke(query);
            }
            else if(whenFalse is not null)
            {
                query = whenFalse.Invoke(query);
            }

            return query;
        }

        /// <summary>
        /// Сортировка елементов.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="order">Строка "+PropertyName1,-PropertyName2,+PropertyName3"</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IQueryable<T> OrderByStr<T>(this IQueryable<T> source, string? order)
        {
            if (string.IsNullOrEmpty(order)) return source;
        
            Regex regex = new(@"[+-]?\w+");

            MatchCollection matches = regex.Matches(order);

            if (matches is null || matches.Any() == false) throw new Exception("Неверный формат строки сортировки!");

            List<SortParam> sortParams = new();
            foreach (Match match in matches)
            {
                string propertyName = "";
                SortParam param = new();
                if (match.Value.StartsWith("+"))
                {
                    param.IsAscending = true;
                    param.PropertyName = match.Value.Substring(1);
                }
                else if (match.Value.StartsWith("-"))
                {
                    param.IsAscending = false;
                    param.PropertyName = match.Value.Substring(1);
                }
                else
                {
                    param.IsAscending = true;
                    param.PropertyName = match.Value;
                }
                sortParams.Add(param);
            }

            return Order(source, sortParams);
        }
    
        public static IQueryable<T> Order<T>(this IQueryable<T> source, List<SortParam> sorting)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (sorting.Count == 0)
                throw new ArgumentException("Сортировка не может быть пустой", nameof(sorting));

            for (var i = 0; i < sorting.Count; i++)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(param, sorting[i].PropertyName);
                var lambda = Expression.Lambda(property, param);

                var methodName = sorting[i].IsAscending 
                    ? (i == 0 ? "OrderBy" : "ThenBy")
                    : (i == 0 ? "OrderByDescending" : "ThenByDescending");

                var resultExpression = Expression.Call(typeof(Queryable), methodName, new Type[] { source.ElementType, property.Type },
                    source.Expression, Expression.Quote(lambda));

                source = source.Provider.CreateQuery<T>(resultExpression);
            }

            return source;
        }
    
        public class SortParam
        {
            /// <summary>
            /// Наименование поля для сортировки
            /// </summary>
            public string PropertyName { get; set; } = null!;

            /// <summary>
            /// По возрастанию (true) или по убыванию (false)
            /// </summary>
            public bool IsAscending { get; set; } = true;
        }
    }
}