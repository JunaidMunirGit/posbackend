using Microsoft.AspNetCore.Identity;
using Pos.Api.Security;
using Pos.Application;
using Pos.Domain.Entities;
using Pos.Infrastructure;


var builder = WebApplication.CreateBuilder(args);


// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean Architecture DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


// Auth helpers
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Middleware DI
builder.Services.AddScoped<SimpleIpRateLimitMiddleware>();

builder.Services.AddControllers();



var app = builder.Build();

app.UseMiddleware<SimpleIpRateLimitMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();