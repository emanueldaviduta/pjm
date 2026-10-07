using ProjectManager.Api.Endpoints;
using ProjectManager.Api.Extensions;
using ProjectManager.Application;
using ProjectManager.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Validates DataAnnotations on endpoint parameters (replaces [ApiController] model validation).
builder.Services.AddValidation();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

if(builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod()
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyHeader()
    .AllowAnyMethod());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "PJM API V1"));
}

app.Services.ApplyMigrations();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapAccountEndpoints();
app.MapProjectEndpoints();
app.MapTaskItemEndpoints();
app.MapRoleEndpoints();
app.MapProjectMembershipEndpoints();

app.Run();
