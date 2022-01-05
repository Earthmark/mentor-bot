using MentorBot;
using MentorBot.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MentorOptions>(builder.Configuration.GetSection("mentors"));

builder.Services.AddSingleton<ITicketNotifier, TicketNotifier>();

builder.Services.AddDiscordContext(builder.Configuration);

builder.Services.AddNeosHttpClient(builder.Configuration);

builder.Services.AddSignalContexts(builder.Configuration);

builder.Services.AddTransient<ITokenGenerator, TokenGenerator>();

builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mentor Signal", Version = "v1" }));

builder.Services.AddHealthChecks()
  .AddDiscordCheck()
  .AddSignalHealthChecks();

builder.Services.Configure<JsonOptions>(options =>
{
  options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddControllers().AddJsonOptions(c =>
{
  c.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddAuthentication(c =>
{
  c.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(c =>
{
  c.ExpireTimeSpan = TimeSpan.FromHours(3);
});

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

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mentor Signal v1"));

app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapHealthChecks("/health");
app.MapControllers();
app.MapRazorPages();
app.MapSwagger();

app.Run();