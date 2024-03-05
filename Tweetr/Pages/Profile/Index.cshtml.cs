using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Profile
{
    public class IndexModel : PageModel
    {
        public User User { get; set; } = default!;
        public bool IsAccount { get; set; } = false;
        public IList<Post> Posts { get; set; } = default!;
        [BindProperty]
        public IFormFile? ProfileImageUpload { get; set; }

        public string UploadStatus { get; set; } = "";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> OnGetAsync(string? viewedUsername, string? uploadStatus)
        {
            var sessionUsername = HttpContext.Session.GetString("username");
            var username = viewedUsername;
            username ??= sessionUsername;

            if (username == null)
            {
                return RedirectToPage("/Login/Index");
            }

            var uploadStatusInfo = uploadStatus;
            if (viewedUsername == sessionUsername)
            {
                return RedirectToPage("Index", new { uploadStatus = uploadStatusInfo });
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

            if (uploadStatus != null)
            {
                UploadStatus = uploadStatus;
            }

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

            // Profile Image
            if (ProfileImageUpload != null)
            {
                string profileImageLocation = Path.Combine(webRootPath, @"images/profile");
                string extension = Path.GetExtension(ProfileImageUpload.FileName).ToLower();

                string[] imageFileTypes = [".jpeg", ".jpg", ".png"];
                if (!imageFileTypes.Contains(extension))
                {
                    ModelState.AddModelError("ProfileImageUpload", "Valid image file types: .jpeg, .jpg, .png");
                    return RedirectToPage("Index", new { viewedUsername = username, uploadStatus = "Valid image file types: .jpeg, .jpg, .png" });
                }

                using (var fileStream = new FileStream(Path.Combine(profileImageLocation, username + extension), FileMode.Create))
                {
                    await ProfileImageUpload.CopyToAsync(fileStream);
                }

                user.ProfileImageUrl = @"images/profile/" + username + extension;

                _context.Attach(user).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (! await _context.Users.AnyAsync(u => u.Id == user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Cover Image

            // Name

            // Bio

            return RedirectToPage("Index", new { viewedUsername = username });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login/Index");
        }
    }
}
