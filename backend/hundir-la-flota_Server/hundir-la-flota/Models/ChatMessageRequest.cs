namespace hundir_la_flota.Models
{
    public class ChatMessageRequest
    {
        public int SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

}
