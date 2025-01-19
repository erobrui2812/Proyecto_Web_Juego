namespace hundir_la_flota.DTOs
{
    public class FriendRequestDto
    {
        public string Nickname { get; set; }
        public string Email { get; set; }
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderNickname { get; set; }
        public int RecipientId { get; set; }
        public string RecipientNickname { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
