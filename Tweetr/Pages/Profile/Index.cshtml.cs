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
            }

            var loggedInUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username.ToString()));
            if (loggedInUser == null)
            {
                return RedirectToPage("/Login/Index");
            }

            User = loggedInUser;
            Name = User.Name;
            Bio = User.Bio;

            if (HttpContext.Session.GetString("hasCoverImageError") != null)
            {
                ModelState.AddModelError("CoverImageUpload", "Valid image file types: .jpeg, .jpg, .png");
                HttpContext.Session.Remove("hasCoverImageError");
            }
            if (HttpContext.Session.GetString("hasProfileImageError") != null)
            {
                ModelState.AddModelError("ProfileImageUpload", "Valid image file types: .jpeg, .jpg, .png");
                HttpContext.Session.Remove("hasProfileImageError");
            }
            if (HttpContext.Session.GetString("hasNameError") != null)
            {
                ModelState.AddModelError("Name", "Name cannot be empty");
                HttpContext.Session.Remove("hasNameError");
            }
            // Other errors here

            Posts = await _context.Posts
                    .Where(p => p.Username.Equals(User.Username))
                    .OrderByDescending(p => p.DatePosted).ToListAsync();

            return Page();
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

            // Cover Image
            if (CoverImageUpload != null)
            {
                string coverImageLocation = Path.Combine(webRootPath, @"images/cover");
                string extension = Path.GetExtension(CoverImageUpload.FileName).ToLower();

                if (!imageFileTypes.Contains(extension))
                {
                    HttpContext.Session.SetString("hasCoverImageError", "true");
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
            if (ProfileImageUpload != null)
            {
                string profileImageLocation = Path.Combine(webRootPath, @"images/profile");
                string extension = Path.GetExtension(ProfileImageUpload.FileName).ToLower();

                if (!imageFileTypes.Contains(extension))
                {
                    HttpContext.Session.SetString("hasProfileImageError", "true");
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
                }
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
            else
            {
                HttpContext.Session.SetString("hasNameError", "true");
            }

            // Bio
            // -> ok even if null or empty

            return RedirectToPage("Index", new { viewedUsername = username });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login/Index");
        }
    }
}
