using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Posts
{
    public class DetailsModel : PageModel
    {
        // Public properties
        public Post Post { get; set; } = default!; // Use only in OnGet()

        [BindProperty]
        public string? Comment { get; set; }

        [BindProperty]
        public string? EditPostText { get; set; }

        public IList<Comment> Comments { get; set; } = default!;

        public bool IsOwnPost { get; set; } = false;

        public bool IsLiked { get; set; } = false;
        public bool AlreadyReposted { get; set; } = false;

        // Private properties
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Handler Methods
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id || p.OriginalPostId == id);
            if (post == null)
            {
                return NotFound();
            }
            else
            {
                Post = post;
                EditPostText = Post.Content;

                Comments = await _context.Comments
                        .Where(c => c.PostId == Post.Id)
                        .OrderBy(c => c.DatePosted)
                        .ToListAsync();

                var username = HttpContext.Session.GetString("username")?.ToString();

                if (username != null)
                {
                    if (username.Equals(Post.Username) || username.Equals(Post.RepostedBy))
                    {
                        IsOwnPost = true;
                    }

                    var like = await _context.Likes.FirstOrDefaultAsync(l => l.Username == username && l.PostId == Post.Id);
                    if (like != null)
                    {
                        IsLiked = true;
                    }

                    if (await _context.Posts.AnyAsync(p => (p.Id == Post.Id || p.OriginalPostId == Post.Id) && p.RepostedBy != null && p.RepostedBy.Equals(username)))
                    {
                        AlreadyReposted = true;
                    }
                }
            }
            return Page();
        }

        // Comment
        public async Task<IActionResult> OnPostCommentAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            Comments = await _context.Comments
                        .Where(c => c.PostId == post.Id)
                        .OrderBy(c => c.DatePosted)
                        .ToListAsync();

            if (Comment == null)
            {
                ModelState.AddModelError("Comment", "Please type something first.");
                return Page();
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username));
                if (user == null)
                {
                    return NotFound();
                }

                var comment = new Comment
                {
                    Name = user.Name,
                    Username = user.Username,
                    Content = Comment,
                    DatePosted = DateTime.UtcNow,
                    PostId = post.Id,
                    ProfileImageUrl = user.ProfileImageUrl
                };

                _context.Comments.Add(comment);

                post.TotalComments += 1;
                _context.Attach(post).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Posts.AnyAsync(p => p.Id == post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return RedirectToPage("Details", new { id = post.Id });
        }

        // Like
        public async Task<IActionResult> OnPostLikeAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            var like = new Like
            {
                PostId = post.Id,
                Username = username,
                DateLiked = DateTime.UtcNow
            };

            _context.Likes.Add(like);

            post.TotalLikes += 1;
            _context.Attach(post).State = EntityState.Modified;

            // Update reposts
            var reposts = await _context.Posts.Where(p => p.OriginalPostId == post.Id).ToListAsync();
            foreach (var repost in reposts)
            {
                repost.TotalLikes = post.TotalLikes;
                _context.Attach(repost).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Posts.AnyAsync(p => p.Id == post.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("Details", new { id = post.Id });
        }

        // Unlike
        public async Task<IActionResult> OnPostUnlikeAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == post.Id && l.Username.Equals(username));
            if (like == null)
            {
                return NotFound();
            }

            _context.Likes.Remove(like);

            post.TotalLikes -= 1;
            _context.Attach(post).State = EntityState.Modified;

            // Update reposts
            var reposts = await _context.Posts.Where(p => p.OriginalPostId == post.Id).ToListAsync();
            foreach (var repost in reposts)
            {
                repost.TotalLikes = post.TotalLikes;
                _context.Attach(repost).State = EntityState.Modified;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Posts.AnyAsync(p => p.Id == post.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("Details", new { id = post.Id });
        }

        // Delete
        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            // Related comments
            var relatedComments = await _context.Comments.Where(c => c.PostId == post.Id).ToListAsync();
            foreach (var rc in relatedComments)
            {
                _context.Comments.Remove(rc);
            }

            // Related likes
            var relatedLikes = await _context.Likes.Where(l => l.PostId == post.Id).ToListAsync();
            foreach (var rl in relatedLikes)
            {
                _context.Likes.Remove(rl);
            }

            // TODO: Related reposts

            // Delete post itself
            _context.Posts.Remove(post);

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        // Edit
        public async Task<IActionResult> OnPostEditAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            Post = post;

            if (EditPostText == null)
            {
                return Page();
            }

            Post.Content = EditPostText;
            Post.DateEdited = DateTime.UtcNow;

            _context.Attach(Post).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(Post.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("Details", new { id = Post.Id });
        }

        public async Task<IActionResult> OnPostRepostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var username = HttpContext.Session.GetString("username")?.ToString();
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var originalPost = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);
            if (originalPost == null)
            {
                return NotFound();
            }

            originalPost.TotalReposts += 1;
            _context.Posts.Attach(originalPost).State = EntityState.Modified;

            // Update all reposts of the original post
            var allReposts = await _context.Posts.Where(p => p.OriginalPostId == originalPost.Id).ToListAsync();
            foreach (var repost in allReposts)
            {
                repost.TotalReposts = originalPost.TotalReposts;
                _context.Attach(repost).State = EntityState.Modified;
            }

            var postCopy = new Post
            {
                Name = originalPost.Name,
                Username = originalPost.Username,
                DatePosted = originalPost.DatePosted,
                DateEdited = originalPost.DateEdited,
                Content = originalPost.Content,
                TotalLikes = originalPost.TotalLikes,
                TotalComments = originalPost.TotalComments,
                TotalReposts = originalPost.TotalReposts,
                ProfileImageUrl = originalPost.ProfileImageUrl,
                OriginalPostId = originalPost.Id,
                RepostedBy = username,
                DateReposted = DateTime.UtcNow
            };

            _context.Posts.Add(postCopy);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("repostSuccessful", "true");

            return RedirectToPage("/Posts/Index");
        }

        #endregion

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
