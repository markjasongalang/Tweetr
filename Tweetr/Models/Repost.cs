namespace Tweetr.Models
{
    public class Repost
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Username { get; set; } = null!;
        public DateTime DateReposted { get; set; }
    }
}
