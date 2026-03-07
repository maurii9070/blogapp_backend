using Blog.Api.Data;
using Blog.Api.Entities;
using Blog.Api.Extensions;
using Blog.Api.Features.Users;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BlogDbContext>(options =>
{
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
       .AddCookie(IdentityConstants.ApplicationScheme, options =>
       {
           options.Cookie.Name = "BlogAuthCookie";
           options.ExpireTimeSpan = TimeSpan.FromDays(7);
           options.SlidingExpiration = true;
           options.Cookie.HttpOnly = true;

           options.Events.OnRedirectToLogin = context =>
           {
               context.Response.StatusCode = StatusCodes.Status401Unauthorized;
               return Task.CompletedTask;
           };
           options.Events.OnRedirectToAccessDenied = context =>
           {
               context.Response.StatusCode = StatusCodes.Status403Forbidden;
               return Task.CompletedTask;
           };
       });

builder.Services
       .AddIdentityCore<ApplicationUser>(options =>
       {
           options.Password.RequireLowercase = false;
           options.Password.RequireUppercase = false;
           options.Password.RequireDigit = false;
           options.Password.RequiredLength = 6;
           options.Password.RequireNonAlphanumeric = false;
           options.User.RequireUniqueEmail = true;
       })
       .AddRoles<IdentityRole>()
       .AddEntityFrameworkStores<BlogDbContext>()
       .AddSignInManager<SignInManager<ApplicationUser>>()
       .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<UserRegistration.Handler>();
builder.Services.AddScoped<UserLogin.Handler>();
builder.Services.AddScoped<UserLogout.Handler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedRoles(scope.ServiceProvider);
}

app.MapUserEndpoints();

app.Run();