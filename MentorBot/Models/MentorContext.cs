using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MentorBot.Extern;
using Microsoft.EntityFrameworkCore;

namespace MentorBot.Models;

public interface IMentorContext
{
    IAsyncEnumerable<Mentor> Mentors();
    ValueTask<Mentor?> GetMentorByResoIdAsync(string resoUserId, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> GetMentorByTokenAsync(string token, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> AddMentorAsync(string resoUserId, CancellationToken cancellationToken = default);
    ValueTask<Mentor?> RemoveMentorAccess(string resoUserId, CancellationToken cancellationToken = default);
}

public class MentorContext : IMentorContext
{
    private readonly ISignalContext _ctx;
    private readonly IResoniteApi _neosApi;
    private readonly ITokenGenerator _tokenGen;

    public MentorContext(ISignalContext ctx, IResoniteApi neosApi, ITokenGenerator tokenGen)
    {
        _ctx = ctx;
        _neosApi = neosApi;
        _tokenGen = tokenGen;
    }

    public IAsyncEnumerable<Mentor> Mentors()
    {
        return _ctx.Mentors.AsAsyncEnumerable();
    }

    public async ValueTask<Mentor?> GetMentorByResoIdAsync(string resoUserId, CancellationToken cancellationToken)
    {
        return await _ctx.Mentors.SingleOrDefaultAsync(t => t.ResoUserId == resoUserId, cancellationToken);
    }

    public async ValueTask<Mentor?> GetMentorByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return await _ctx.Mentors.SingleOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async ValueTask<Mentor?> AddMentorAsync(string resoUserId, CancellationToken cancellationToken)
    {
        var mentorUserTask = _neosApi.GetUserAsync(resoUserId, cancellationToken);
        var existingMentorTask = GetMentorByResoIdAsync(resoUserId, cancellationToken);

        var mentorUser = await mentorUserTask;
        if (mentorUser == null) return null;

        var mentor = await existingMentorTask;
        if (mentor == null)
        {
            mentor = new Mentor
            {
                ResoUserId = resoUserId,
                Name = mentorUser.Name
            };
            _ctx.Add(mentor);
        }

        mentor.Token = _tokenGen.CreateToken();

        await _neosApi.SetCloudVarAuthTokenAsync(mentor.Token, resoUserId, cancellationToken);

        await _ctx.SaveChangesAsync(cancellationToken);
        return mentor;
    }

    public async ValueTask<Mentor?> RemoveMentorAccess(string resoUserId, CancellationToken cancellationToken)
    {
        var mentor = await GetMentorByResoIdAsync(resoUserId, cancellationToken);
        if (mentor != null)
        {
            mentor.Token = null;
            _ctx.Update(mentor);
            await _ctx.SaveChangesAsync(cancellationToken);
        }

        return mentor;
    }
}