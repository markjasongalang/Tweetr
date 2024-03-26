using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Profile
{
    public class IndexModel : PageModel
    {
        // Public properties
        public User User { get; set; } = default!;
        public bool IsAccount { get; set; } = false;
        public IList<Post> Posts { get; set; } = default!;
        [BindProperty]
        public IFormFile? CoverImageUpload { get; set; }
        [BindProperty]
        public IFormFile? ProfileImageUpload { get; set; }
        [BindProperty]
        public string? Name { get; set; }
        [BindProperty]
        public string? Bio { get; set; }
        public string EditProfileStatus { get; set; } = "default";
        [BindProperty]
        public bool RemoveCoverImage { get; set; } = false;
        [BindProperty]
        public bool RemoveProfileImage { get; set; } = false;
        public string? ViewedUser { get; set; }
        public bool IsFollowed { get; set; } = false;

        // Private fields
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> OnGetAsync(string? viewedUsername)
        {
            var sessionUsername = HttpContext.Session.GetString("username");
            var username = viewedUsername;
            username ??= sessionUsername;

            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            if (viewedUsername == sessionUsername)
            {
                return RedirectToPage("Index");
            }

            if (viewedUsername == null)
            {
                IsAccount = true;
            } else
            {
                ViewedUser = viewedUsername;

                // Check if already followed
                var current = HttpContext.Session.GetString("username");
                if (current != null && await _context.Follows.AnyAsync(f => f.Following.Equals(ViewedUser) && f.Follower.Equals(current)))
                {
                    IsFollowed = true;
                }
            }

            var loggedInUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username.ToString()));
            if (loggedInUser == null)
            {
                return RedirectToPage("/Login/Index");
            }

            User = loggedInUser;
            Name = User.Name;
            Bio = User.Bio;

            if (HttpContext.Session.GetString("editProfileSuccess") != null)
            {
                HttpContext.Session.Remove("editProfileSuccess");
                EditProfileStatus = "success";
            }

            if (HttpContext.Session.GetString("hasCoverImageError") != null)
            {
                ModelState.AddModelError("CoverImageUpload", "Valid image file types: .jpeg, .jpg, .png");
                HttpContext.Session.Remove("hasCoverImageError");
                EditProfileStatus = "error";
            }
            if (HttpContext.Session.GetString("hasProfileImageError") != null)
            {
                ModelState.AddModelError("ProfileImageUpload", "Valid image file types: .jpeg, .jpg, .png");
                HttpContext.Session.Remove("hasProfileImageError");
                EditProfileStatus = "error";
            }
            if (HttpContext.Session.GetString("hasNameError") != null)
            {
                ModelState.AddModelError("Name", "Name cannot be empty");
                HttpContext.Session.Remove("hasNameError");
                EditProfileStatus = "error";
            }

            Posts = await _context.Posts
                    .Where(p => (p.Username.Equals(User.Username) && p.OriginalPostId == null) || 
                        (!string.IsNullOrEmpty(p.RepostedBy) && p.RepostedBy.Equals(User.Username)))
                    .OrderByDescending(p => p.DatePosted)
                    .ThenByDescending(p => p.DateReposted)
                    .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostFollowAsync(string? viewedUser)
        {
            var username = HttpContext.Session.GetString("username");
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            if (viewedUser == null)
            {
                return NotFound();
            }

            var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(viewedUser));
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username));
            if (recipient == null || sender == null)
            {
                return NotFound();
            }

            var follow = new Follow
            {
                Following = recipient.Username,
                Follower = sender.Username,
                DateFollowed = DateTime.UtcNow
            };

            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index", new { viewedUsername = viewedUser });
        }

        public async Task<IActionResult> OnPostUnfollowAsync(string? viewedUser)
        {
            var username = HttpContext.Session.GetString("username");
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            if (viewedUser == null)
            {
                return NotFound();
            }

            var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(viewedUser));
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username));
            if (recipient == null || sender == null)
            {
                return NotFound();
            }

            var follow = await _context.Follows.FirstOrDefaultAsync(f => f.Following.Equals(recipient.Username) && f.Follower.Equals(sender.Username));
            if (follow == null)
            {
                return NotFound();
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index", new { viewedUsername = viewedUser });
        }

        public async Task<IActionResult> OnPostEditProfileAsync(string? username)
        {
            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username));
            if (user == null)
            {
                return RedirectToPage("/Login/Index");
            }

            string webRootPath = _webHostEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            string[] imageFileTypes = [".jpeg", ".jpg", ".png"];

            bool editSuccess = true;

            // Cover Image
            if (RemoveCoverImage && user.CoverImageUrl != null)
            {
                // Remove file from wwwroot
                string coverImageFilePath = Path.Combine(webRootPath, user.CoverImageUrl);
                if (System.IO.File.Exists(coverImageFilePath))
                {
                    System.IO.File.Delete(coverImageFilePath);
                }

                // Remove path in database
                user.CoverImageUrl = null;
                _context.Attach(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            else if (CoverImageUpload != null)
            {
                string coverImageLocation = Path.Combine(webRootPath, @"images/cover");
                string extension = Path.GetExtension(CoverImageUpload.FileName).ToLower();

                if (!imageFileTypes.Contains(extension))
                {
                    HttpContext.Session.SetString("hasCoverImageError", "true");
                    editSuccess = false;
                }

                if (HttpContext.Session.GetString("hasCoverImageError") == null)
                {
                    using FileStream fileStream = new(Path.Combine(coverImageLocation, username + extension), FileMode.Create);
                    await CoverImageUpload.CopyToAsync(fileStream);

                    user.CoverImageUrl = @"images/cover/" + username + extension;

                    _context.Attach(user).State = EntityState.Modified;
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await _context.Users.AnyAsync(u => u.Id == user.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            // Profile Image
            bool profilePictureUpdated = false;
            if (RemoveProfileImage && user.ProfileImageUrl != null)
            {
                // Remove file from wwwroot
                string profileImageFilePath = Path.Combine(webRootPath, user.ProfileImageUrl);
                if (System.IO.File.Exists(profileImageFilePath))
                {
                    System.IO.File.Delete(profileImageFilePath);
                }

                // Set default profile image path in database
                user.ProfileImageUrl = @"images/profile/default_profile_image.jpg";
                _context.Attach(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                profilePictureUpdated = true;
            }
            else if (ProfileImageUpload != null)
            {
                string profileImageLocation = Path.Combine(webRootPath, @"images/profile");
                string extension = Path.GetExtension(ProfileImageUpload.FileName).ToLower();

                if (!imageFileTypes.Contains(extension))
                {
                    HttpContext.Session.SetString("hasProfileImageError", "true");
                    editSuccess = false;
                }

                if (HttpContext.Session.GetString("hasProfileImageError") == null)
                {
                    using FileStream fileStream = new(Path.Combine(profileImageLocation, username + extension), FileMode.Create);
                    await ProfileImageUpload.CopyToAsync(fileStream);

                    user.ProfileImageUrl = @"images/profile/" + username + extension;

                    _context.Attach(user).State = EntityState.Modified;
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await _context.Users.AnyAsync(u => u.Id == user.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }

                    profilePictureUpdated = true;
                }
            }

            if (profilePictureUpdated)
            {
                // Update profile picture in posts
                var posts = await _context.Posts.Where(p => p.Username.Equals(user.Username)).ToListAsync();
                foreach (var post in posts)
                {
                    post.ProfileImageUrl = user.ProfileImageUrl;
                    _context.Attach(post).State = EntityState.Modified;
                }

                // Update profile picture in comments
                var comments = await _context.Comments.Where(c => c.Username.Equals(user.Username)).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.ProfileImageUrl = user.ProfileImageUrl;
                    _context.Attach(comment).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
            }

            // Name
            if (Name != null && !Name.Equals(user.Name))
            {
                user.Name = Name;
                _context.Attach(user).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Users.AnyAsync(u => u.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                // Update name of owned posts
                var ownedPosts = await _context.Posts.Where(p => p.Username == user.Username).ToListAsync();
                foreach (var post in ownedPosts)
                {
                    post.Name = user.Name;
                    _context.Attach(post).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(Name))
            {
                HttpContext.Session.SetString("hasNameError", "true");
                editSuccess = false;
            }

            // Bio
            if (Bio != null && !Bio.Equals(user.Bio))
            {
                user.Bio = Bio;
                _context.Attach(user).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Users.AnyAsync(u => u.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (Bio == null)
            {
                user.Bio = null;
                _context.Attach(user).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Users.AnyAsync(u => u.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (editSuccess)
            {
                HttpContext.Session.SetString("editProfileSuccess", "true");
            }

            return RedirectToPage("Index", new { viewedUsername = username });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login/Index");
        }
    }
}
