using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;

namespace Tweetr.Pages.Login
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        [BindProperty]
        public required string Username { get; set; }
        [BindProperty]
        public required string Password { get; set; }

        public string? Message { get; set; }

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
            if (Username == null || Password == null)
            {
                Message = "Please fill in all fields.";
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username);
            if (user == null || Password != user.Password)
            {
                Message = "Incorrect username or password.";
                return Page();
            }

            // Set session
            HttpContext.Session.SetString("username", user.Username);

            return RedirectToPage("/Posts/Index");
        }
    }
}
