using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Posts
{
    public class IndexModel : PageModel
    {
        // Public properties
        public IList<Post> Posts { get;set; } = default!;
        public bool IsLoggedIn { get; set; }

        [BindProperty]
        public Post Post { get; set; } = default!;
        public bool RepostSuccessful { get; set; } = false;

        // Private properties
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            IsLoggedIn = HttpContext.Session.GetString("username") != null;
            Posts = await _context.Posts
                .OrderByDescending(p => p.DatePosted)
                .ThenByDescending(p => p.DateReposted)
                .ToListAsync();

            if (HttpContext.Session.GetString("repostSuccessful") != null)
            {
                RepostSuccessful = true;
                HttpContext.Session.Remove("repostSuccessful");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var username = HttpContext.Session.GetString("username");
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var loggedInUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username));
            if (loggedInUser == null)
            {
                return RedirectToPage("/Login/Index");
            }

            if (string.IsNullOrEmpty(Post.Content))
            {
                return Page();
            }

            Post.Name = loggedInUser.Name;
            Post.Username = loggedInUser.Username;
            Post.DatePosted = DateTime.UtcNow;
            Post.DateEdited = Post.DatePosted;
            Post.TotalLikes = 0;
            Post.TotalComments = 0;
            Post.TotalReposts = 0;
            Post.ProfileImageUrl = loggedInUser.ProfileImageUrl;

            _context.Posts.Add(Post);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
