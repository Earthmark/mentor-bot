using MentorBot;
using MentorBot.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

var mentorConfig = builder.Configuration.GetSection("mentors");
builder.Services.Configure<MentorOptions>(mentorConfig);
var hasSwagger = mentorConfig.Get<MentorOptions>()?.EnableSwagger ?? false;

builder.Services.AddSingleton<ITicketNotifier, TicketNotifier>();

builder.Services.AddDiscordContext(builder.Configuration);

builder.Services.AddNeosHttpClient(builder.Configuration);

builder.Services.AddSignalContexts(builder.Configuration);

builder.Services.AddTransient<ITokenGenerator, TokenGenerator>();

if (hasSwagger)
{
  builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));
}

builder.Services.AddHealthChecks()
  .AddDiscordCheck()
  .AddSignalHealthChecks();

builder.Services.Configure<JsonOptions>(options =>
  options.SerializerOptions.ConfigureForMentor());

builder.Services.AddControllers().AddJsonOptions(opts =>
  opts.JsonSerializerOptions.ConfigureForMentor());

builder.Services.AddAuthentication(c => c.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(c => c.ExpireTimeSpan = TimeSpan.FromHours(3));

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
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

if (hasSwagger)
{
  app.UseSwagger();
  app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentor Signal v1"));
}

app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapHealthChecks("/health");
app.MapControllers();
app.MapRazorPages();

if (hasSwagger)
{
  app.MapSwagger();
}

app.Run();

// This is needed so integration tests succeed.
public partial class Program { }
