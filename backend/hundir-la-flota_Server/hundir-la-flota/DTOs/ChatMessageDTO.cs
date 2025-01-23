namespace hundir_la_flota.DTOs
{
    public class ChatMessageDTO
    {
        public int SenderId { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
    }
}
