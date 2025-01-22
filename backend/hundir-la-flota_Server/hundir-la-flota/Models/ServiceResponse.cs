namespace hundir_la_flota.Models
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public int StatusCode { get; set; } = 200;

        public static ServiceResponse<T> SuccessResponse(T data, string message = null) =>
            new ServiceResponse<T> { Data = data, Success = true, Message = message, StatusCode = 200 };

        public static ServiceResponse<T> ErrorResponse(string message, int statusCode = 400) =>
            new ServiceResponse<T> { Success = false, Message = message, StatusCode = statusCode };
    }
}
