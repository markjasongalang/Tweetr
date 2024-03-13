using System.ComponentModel.DataAnnotations;

namespace Tweetr.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Content { get; set; } = null!;
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DatePosted { get; set; }
        public int PostId { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
