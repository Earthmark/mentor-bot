using MentorBot.Extern;
using MentorBot.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

var builder = WebApplication.CreateBuilder();

builder.Services.AddDiscordContext(builder.Configuration);
builder.Services.AddHostedService<ReactionProcessor>();

builder.Services.AddNeosHttpClient();

builder.Services.AddDbContext<SignalContext>(o =>
  o.UseSqlServer(builder.Configuration.GetConnectionString("SqlDb")));


builder.Services.AddTransient<ITicketContext, TicketContext>()
  .AddTransient<ITicketStore, TicketStore>()
  .AddTransient<IDiscordReactionHandler, TicketStore>();

builder.Services.AddSingleton<ITicketNotifier, TicketNotifier>();

builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));
builder.Services.AddHealthChecks()
  .AddCheck<DiscordHealthCheck>("discord")
  .AddDbContextCheck<SignalContext>();
builder.Services.AddControllers();

builder.Services.AddRazorPages();

var app = builder.Build();

app.EnsureDatabaseCreated();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/error");
  app.UseHsts();
  app.UseHttpsRedirection();
}
else
{
  app.UseDeveloperExceptionPage();
  app.UseSwagger();
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentor Signal v1"));
}

app.UseRouting();
app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.UseEndpoints(endpoints =>
{
  endpoints.MapHealthChecks("/health");
  endpoints.MapControllers();
});

app.MapRazorPages();

app.Run();