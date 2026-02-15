using System;
using MentorBot.Models;
using Microsoft.EntityFrameworkCore;

namespace MentorBot.Tests.Models;

internal class TestSignalContext
{
    public TestSignalContext(string fileName, Action<ISignalContext> setup = null)
    {
        ContextOptions = new DbContextOptionsBuilder<SignalContext>()
            .UseSqlite($"Filename={fileName}.db").Options;
        using SignalContext ctx = new(ContextOptions);
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();
        setup?.Invoke(ctx);
    }

    protected DbContextOptions<SignalContext> ContextOptions { get; }

    public ISignalContext CreateContext()
    {
        return new SignalContext(ContextOptions);
    }
}