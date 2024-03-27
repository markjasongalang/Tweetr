using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Signup
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public User User { get; set; } = default!;

        [BindProperty]
        [Display(Name = "Confirm Password")]
        public required string ConfirmPassword { get; set; }

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        /** Handler Methods **/
        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("username") != null)
            {
                return RedirectToPage("/Posts/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            User.DateJoined = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Username == User.Username))
            {
                ModelState.AddModelError("User.Username", "Username already exists.");
                return Page();
            }

            if (ConfirmPassword == null)
            {
                ModelState.AddModelError("ConfirmPassword", "Confirm Password field is required.");
                return Page();
            }

            if (!User.Password.Equals(ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                return Page();
            }

            User.Password = BCrypt.Net.BCrypt.EnhancedHashPassword(User.Password);

            User.ProfileImageUrl = @"images/profile/default_profile_image.jpg";
            User.CoverImageUrl = null;

            _context.Users.Add(User);
            await _context.SaveChangesAsync();

            // Set session
            HttpContext.Session.SetString("username", User.Username);

            return RedirectToPage("/Posts/Index");
        }
    }
}
