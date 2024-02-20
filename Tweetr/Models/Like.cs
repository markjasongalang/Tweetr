namespace Tweetr.Models
{
    public class Like
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Username { get; set; } = null!;
        public DateTime DateLiked { get; set; }
    }
}
