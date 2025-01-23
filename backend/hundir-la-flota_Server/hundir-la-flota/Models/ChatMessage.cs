namespace hundir_la_flota.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public Guid GameId { get; set; }
        public int SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

}
