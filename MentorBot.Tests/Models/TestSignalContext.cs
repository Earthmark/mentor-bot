using MentorBot.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace MentorBot.Tests.Models
{
  internal class TestSignalContext
  {
    protected DbContextOptions<SignalContext> ContextOptions { get; }

    public TestSignalContext(string fileName, Action<ISignalContext> setup = null)
    {
      ContextOptions = new DbContextOptionsBuilder<SignalContext>()
        .UseSqlite($"Filename={fileName}.db").Options;
      using SignalContext ctx = new(ContextOptions);
      ctx.Database.EnsureDeleted();
      ctx.Database.EnsureCreated();
      setup?.Invoke(ctx);
    }

    public ISignalContext CreateContext()
    {
      return new SignalContext(ContextOptions);
    }
  }
}
