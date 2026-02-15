using System;
using MentorBot;
using MentorBot.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MentorOptions>().BindConfiguration("Mentors");

builder.Services.AddSingleton<ITicketNotifier, TicketNotifier>();

builder.Services.AddDiscordContext(builder.Configuration);

builder.Services.AddResoniteHttpClient(builder.Configuration);

builder.Services.AddSignalContexts(builder.Configuration);

builder.Services.AddTransient<ITokenGenerator, TokenGenerator>();

if (builder.Environment.IsDevelopment())
    builder.Services.AddOpenApi(o => o.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);

builder.Services.AddHealthChecks()
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

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Mentor Signal v1"));
}

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapHealthChecks("/health");
app.MapControllers();
app.MapRazorPages();

app.Run();