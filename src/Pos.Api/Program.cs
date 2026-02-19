using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Pos.Api.Middleware;
using Pos.Application;
using Pos.Application.Security;
using Pos.Infrastructure;
using Serilog;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext());


var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = jwt["roles"],
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManageProducts",
        policy => policy.Requirements.Add(
            new PermissionRequirement("ManageProducts")));

    options.AddPolicy("ViewProducts",
        policy => policy.Requirements.Add(
            new PermissionRequirement("ViewProducts")));
    options.AddPolicy("ManageUsers", u => u.RequireRole("Admin"));
});



builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


builder.Services.AddScoped<SimpleIpRateLimitMiddleware>();


builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;

        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";          // v1, v1.0, v2
        options.SubstituteApiVersionInUrl = true;    // replaces {version:apiVersion}
    });



builder.Services.AddControllers();


var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseMiddleware<SimpleIpRateLimitMiddleware>();
app.UseMiddleware<Pos.Api.Middleware.ExceptionHandlingMiddleware>();

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

await app.RunAsync();