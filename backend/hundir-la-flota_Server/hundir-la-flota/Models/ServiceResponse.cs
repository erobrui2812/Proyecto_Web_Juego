namespace hundir_la_flota.Models
{
    public class ServiceResponse<T>
    {
        public T Data { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = null;
        public int StatusCode { get; set; } = 200; // Ejemplo: 400 para errores

    }
}
