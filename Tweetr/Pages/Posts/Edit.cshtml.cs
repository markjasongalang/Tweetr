using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tweetr.Data;
using Tweetr.Models;

namespace Tweetr.Pages.Posts
{
    public class EditModel : PageModel
    {
        private readonly Tweetr.Data.ApplicationDbContext _context;

        [BindProperty]
        public Post Post { get; set; } = default!;

        public static Post? PostCopy { get; set; }

        /** Constructor **/

        public EditModel(Tweetr.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        /** Handler Methods **/

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post =  await _context.Posts.FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }
            Post = post;
            PostCopy = (Post)Post.Clone();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Post.Id = PostCopy!.Id;
            Post.Name = PostCopy.Name;
            Post.Username = PostCopy.Username;
            Post.DatePosted = PostCopy.DatePosted;

            if (Post.Content == null)
            {
                return Page();
            }

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

            return RedirectToPage("./Index");
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
