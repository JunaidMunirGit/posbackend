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


builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<SimpleIpRateLimitMiddleware>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseMiddleware<SimpleIpRateLimitMiddleware>();


app.Run();

