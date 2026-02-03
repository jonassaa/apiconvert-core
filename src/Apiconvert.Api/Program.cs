using Apiconvert.Api.Inbound;
using Apiconvert.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type =>
        type.FullName?.Replace('+', '.') ?? type.Name);
});
builder.Services.AddControllers();
builder.Services.AddApiconvertApi();
builder.Services.AddApiconvertInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        var origin = builder.Configuration["CLIENT_ORIGIN"]
            ?? builder.Configuration["NEXT_PUBLIC_SITE_URL"]
            ?? "http://localhost:3123";
        policy.WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var supabaseUrl = builder.Configuration["SUPABASE_URL"]
    ?? builder.Configuration["NEXT_PUBLIC_SUPABASE_URL"];
var supabaseAudience = builder.Configuration["SUPABASE_JWT_AUDIENCE"] ?? "authenticated";

if (string.IsNullOrWhiteSpace(supabaseUrl))
{
    throw new InvalidOperationException("SUPABASE_URL is not configured.");
}

var issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.Audience = supabaseAudience;
        options.RequireHttpsMetadata = !issuer.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = supabaseAudience
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("client");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck")
    ;

app.MapControllers().RequireCors("client");

app.Run();
