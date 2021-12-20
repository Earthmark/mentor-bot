using MentorBot.Extern;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MentorBot.Models
{
  public interface IMentorContext
  {
    IAsyncEnumerable<Mentor> Mentors();
    ValueTask<Mentor?> GetMentorAsync(string neosId, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> GetMentorByTokenAsync(string token, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> AddMentorAsync(string neosId, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> RemoveMentorAccess(string neosId, CancellationToken cancellationToken = default);
  }

  public class MentorContext : IMentorContext
  {
    private readonly SignalContext _ctx;
    private readonly INeosApi _neosApi;
    private readonly ITokenGenerator _tokenGen;

    public MentorContext(SignalContext ctx, INeosApi neosApi, ITokenGenerator tokenGen)
    {
      _ctx = ctx;
      _neosApi = neosApi;
      _tokenGen = tokenGen;
    }

    public IAsyncEnumerable<Mentor> Mentors()
    {
      return _ctx.Mentors.AsAsyncEnumerable();
    }

    public async ValueTask<Mentor?> GetMentorAsync(string neosId, CancellationToken cancellationToken)
    {
      return await _ctx.Mentors.FirstOrDefaultAsync(m => m.NeosId == neosId, cancellationToken);
    }

    public async ValueTask<Mentor?> GetMentorByTokenAsync(string token, CancellationToken cancellationToken)
    {
      return await _ctx.Mentors.FirstOrDefaultAsync(m => m.Token == token, cancellationToken);
    }

    public async ValueTask<Mentor?> AddMentorAsync(string neosId, CancellationToken cancellationToken)
    {
      var mentorUserTask = _neosApi.GetUser(neosId, cancellationToken);
      var existingMentorTask = _ctx.Mentors.SingleOrDefaultAsync(m => m.NeosId == neosId, cancellationToken);

      var mentorUser = await mentorUserTask;
      if (mentorUser == null)
      {
        return null;
      }

      var existingMentor = await existingMentorTask;
      if (existingMentor != null)
      {
        return existingMentor;
      }

      Mentor mentor = new()
      {
        NeosId = neosId,
        Name = mentorUser.Name,
        Token = _tokenGen.CreateToken()
      };

      _ctx.Mentors.Add(mentor);
      await _ctx.SaveChangesAsync(cancellationToken);
      return mentor;
    }

    public async ValueTask<Mentor?> RemoveMentorAccess(string neosId, CancellationToken cancellationToken)
    {
      var mentor = await _ctx.Mentors.SingleOrDefaultAsync(m => m.NeosId == neosId, cancellationToken);
      if(mentor != null)
      {
        mentor.Token = null;
        _ctx.Mentors.Update(mentor);
        await _ctx.SaveChangesAsync(cancellationToken);
      }
      return mentor;
    }
  }
}
