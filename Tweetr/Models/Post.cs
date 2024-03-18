using System.ComponentModel.DataAnnotations;

namespace Tweetr.Models
{
    public class Post : ICloneable
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Username { get; set; } = null!;

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DatePosted { get; set; }

        public DateTime DateEdited { get; set; }

        [Required(ErrorMessage = "Please write something first.")]
        public string Content { get; set; } = null!;

        public int TotalLikes { get; set; }

        public int TotalComments { get; set; }

        public int TotalReposts { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string? RepostedBy { get; set; }

        #region ICloneable Members
        public object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}
