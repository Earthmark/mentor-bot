using MentorBot.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MentorBot.Pages
{
  public class IndexModel : PageModel
  {
    private readonly IMentorContext _mentors;

    public List<MentorDto> Mentors { get; set; } = new();

    public IndexModel(IMentorContext mentors)
    {
      _mentors = mentors;
    }

    public async Task OnGetAsync()
    {
      Mentors = await _mentors.Mentors().Select(m => m.ToDto()).ToListAsync(HttpContext.RequestAborted);
    }
  }
}
