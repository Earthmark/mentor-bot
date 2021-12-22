﻿using MentorBot.Extern;
using System.Collections.Generic;
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
      return await _ctx.GetMentorByNeosIdAsync(neosId, cancellationToken);
    }

    public async ValueTask<Mentor?> GetMentorByTokenAsync(string token, CancellationToken cancellationToken)
    {
      return await _ctx.GetMentorByTokenAsync(token, cancellationToken);
    }

    public async ValueTask<Mentor?> AddMentorAsync(string neosId, CancellationToken cancellationToken)
    {
      var mentorUserTask = _neosApi.GetUserAsync(neosId, cancellationToken);
      var existingMentorTask = _ctx.GetMentorByNeosIdAsync(neosId, cancellationToken);

      var mentorUser = await mentorUserTask;
      if (mentorUser == null)
      {
        return null;
      }

      var mentor = await existingMentorTask;
      if (mentor == null)
      {
        mentor = new()
        {
          NeosId = neosId,
          Name = mentorUser.Name,
        };
        _ctx.Mentors.Add(mentor);
      }
      mentor.Token = _tokenGen.CreateToken();

      await _neosApi.SetCloudVarAuthTokenAsync(mentor.Token, neosId, cancellationToken);

      await _ctx.SaveChangesAsync(cancellationToken);
      return mentor;
    }

    public async ValueTask<Mentor?> RemoveMentorAccess(string neosId, CancellationToken cancellationToken)
    {
      var mentor = await _ctx.GetMentorByNeosIdAsync(neosId, cancellationToken);
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
