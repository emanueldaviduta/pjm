using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.Interfaces;
using WebAPI.Services;
using WebAPI.Extensions;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDb>(options =>
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddIdentityServices(builder.Configuration); 

var app = builder.Build();
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod()
    .WithOrigins("http://localhost:4200", "https://d1lnubzne2r8go.cloudfront.net")
    .AllowAnyHeader()
    .AllowAnyMethod());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

// Add controllers to the services container.

app.MapControllers();

app.Run();
