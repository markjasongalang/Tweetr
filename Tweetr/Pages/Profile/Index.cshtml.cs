using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Profile
{
    public class IndexModel : PageModel
    {
        public User User { get; set; } = default!;
        public bool IsAccount { get; set; } = false;
        public IList<Post> Posts { get; set; } = default!;

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

        public async Task<IActionResult> OnPostUploadAsync(string? username)
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

            if (files.IsNullOrEmpty() || files.Count == 0)
            {
                return RedirectToPage("Index", new { viewedUsername = username, uploadStatus = "No image provided." });
            }

            var profileImageLocation = Path.Combine(webRootPath, @"images/profile");
            var extension = Path.GetExtension(files[0].FileName).ToLower();

            string[] imageFileTypes = [".jpeg", ".jfif", ".jpg", ".pjpeg", ".pjp", ".png"];
            if (!imageFileTypes.Contains(extension))
            {
                return RedirectToPage("/Profile/Index", new { viewedUsername = username, uploadStatus = "Valid image file types: jpeg, .jfif, .jpg, .pjpeg, .pjp, .png" });
            }

            using (var fileStream = new FileStream(Path.Combine(profileImageLocation, username + extension), FileMode.Create))
            {
                files[0].CopyTo(fileStream);
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

            return RedirectToPage("Index", new { viewedUsername = username });
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login/Index");
        }
    }
}
