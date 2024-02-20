namespace Tweetr.Models
{
    public class Follow
    {
        public int Id { get; set; }
        public string Following { get; set; } = null!;
        public string Follower { get; set; } = null!;
        public DateTime DateFollowed { get; set; }
    }
}
