namespace Shared.Contracts
{
    /// <summary>
    /// Представляет результат операции с возможностью успеха или неудачи.
    /// Реализует Railway Oriented Programming паттерн для функциональной обработки ошибок.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Указывает, была ли операция успешной
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Сообщение об ошибке, если операция не была успешной
        /// </summary>
        public string? Error { get; private set; } = string.Empty;

        /// <summary>
        /// Инициализирует новый экземпляр Result
        /// </summary>
        protected Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Создает успешный результат без значения
        /// </summary>
        public static Result Success()
        {
            return new Result(isSuccess: true, string.Empty);
        }

        /// <summary>
        /// Создает успешный результат с значением
        /// </summary>
        /// <typeparam name="T">Тип значения результата</typeparam>
        /// <param name="value">Значение результата</param>
        public static Result<T> Success<T>(T value) where T : class
        {
            return new Result<T>(value, isSuccess: true, string.Empty);
        }

        /// <summary>
        /// Создает неудачный результат с сообщением об ошибке
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        public static Result Failure(string error)
        {
            return new Result(isSuccess: false, error);
        }

        /// <summary>
        /// Создает неудачный результат с типом и сообщением об ошибке
        /// </summary>
        /// <typeparam name="T">Тип значения результата</typeparam>
        /// <param name="error">Сообщение об ошибке</param>
        public static Result<T> Failure<T>(string error) where T : class
        {
            return new Result<T>(null, isSuccess: false, error);
        }

        /// <summary>
        /// Объединяет несколько результатов. Возвращает первый неуспешный результат или Success.
        /// </summary>
        /// <param name="results">Массив результатов для объединения</param>
        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return Success();
        }
    }

    /// <summary>
    /// Представляет результат операции с типизированным значением
    /// </summary>
    /// <typeparam name="T">Тип значения результата</typeparam>
    public class Result<T> : Result
    {
        /// <summary>
        /// Значение результата (null если операция не успешна)
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр Result с значением
        /// </summary>
        public Result(T? value, bool isSuccess = true, string? error = null) : base(isSuccess, error) => Value = value;
    }

    public static class ResultExtensions
    {
        #region Actions in case of success
        public static Result OnSuccess(this Result result, Func<Result> func) => result.IsSuccess ? func() : result;
        public static Result OnSuccess(this Result result, Action action)
        {
            if (result.IsSuccess)
            {
                action();

                return Result.Success();
            }

            return result;
        }
        public static Result OnSuccess<T>(this Result<T> result, Action<T> action)
        {
            if (result.IsSuccess)
            {
                action(result.Value!);

                return Result.Success();
            }

            return result;
        }
        public static Result<T> OnSuccess<T>(this Result result, Func<T> func) where T : class => result.IsSuccess ? Result.Success(func()) : Result.Failure<T>(result.Error!);
        public static Result<T> OnSuccess<T>(this Result result, Func<Result<T>> func) where T : class => result.IsSuccess ? func() : Result.Failure<T>(result.Error!);
        public static Result OnSuccess<T>(this Result<T> result, Func<T, Result> func) => result.IsSuccess ? func(result.Value!) : result;
        #endregion

        #region Actions in case of failure
        public static Result OnFailure(this Result result, Action action)
        {
            if (!result.IsSuccess)
            {
                action();
            }

            return result;
        }
        #endregion

        #region Actions in both cases
        public static Result OnBoth(this Result result, Action<Result> action)
        {
            action(result);

            return result;
        }

        public static T OnBoth<T>(this Result result, Func<Result, T> func) => func(result);
        #endregion
    }
}
