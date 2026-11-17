using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using MiniCRM.Application;
using MiniCRM.Infrastructure;
using MiniCRM.API.Validation;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); builder.Logging.AddJsonConsole();
builder.Services.AddControllers(options=>options.Filters.AddService<FluentValidationActionFilter>());
builder.Services.AddScoped<FluentValidationActionFilter>();builder.Services.AddValidatorsFromAssemblyContaining<CustomerWriteRequestValidator>();
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "MiniCRM API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = Microsoft.OpenApi.Models.ParameterLocation.Header });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement { [new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
builder.Services.AddDbContext<CrmDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddIdentity<CrmUser, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 12; options.Password.RequireDigit = true; options.Password.RequireUppercase = true; options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5; options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true; options.SignIn.RequireConfirmedEmail = false;
}).AddEntityFrameworkStores<CrmDbContext>().AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Set Jwt:SigningKey via environment variable or user-secrets before starting the API.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32) throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MiniCRM"; var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MiniCRM.React";
builder.Services.AddAuthentication(options => { options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = jwtIssuer, ValidateAudience = true, ValidAudience = jwtAudience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = System.Security.Claims.ClaimTypes.Name, RoleClaimType = System.Security.Claims.ClaimTypes.Role };
    options.Events = new JwtBearerEvents { OnTokenValidated = async context =>
    {
        var principal=context.Principal;var userId=principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var jti=principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        var stamp=principal?.FindFirst("minicrm:security_stamp")?.Value;
        if(!int.TryParse(userId,out var id)||string.IsNullOrWhiteSpace(jti)||string.IsNullOrWhiteSpace(stamp)){context.Fail("Invalid access token.");return;}
        var userManager=context.HttpContext.RequestServices.GetRequiredService<UserManager<CrmUser>>();var user=await userManager.FindByIdAsync(id.ToString());
        if(user is null||!user.IsActive||!string.Equals(stamp,await userManager.GetSecurityStampAsync(user),StringComparison.Ordinal)){context.Fail("Access token is revoked.");return;}
        var db=context.HttpContext.RequestServices.GetRequiredService<CrmDbContext>();
        if(await db.RevokedAccessTokens.AnyAsync(x=>x.TokenId==jti&&x.ExpiresAt>DateTime.UtcNow,context.HttpContext.RequestAborted))context.Fail("Access token is revoked.");
    }};
});
builder.Services.AddAuthorization(); builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>(); builder.Services.AddScoped<ICrmService, SqlCrmService>(); builder.Services.AddScoped<TokenService>();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server");
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("api", limiter => { limiter.PermitLimit = 120; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; limiter.AutoReplenishment = true; }));
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>(); var exception = feature?.Error;
    var status = exception switch { FluentValidation.ValidationException => 400, KeyNotFoundException => 404, UnauthorizedAccessException => 403, InvalidOperationException => 409, Microsoft.EntityFrameworkCore.DbUpdateException => 409, _ => 500 };
    var title = exception switch { FluentValidation.ValidationException or InvalidOperationException or KeyNotFoundException => exception.Message, UnauthorizedAccessException => "You are not permitted to perform this action.", Microsoft.EntityFrameworkCore.DbUpdateException => "The request conflicts with existing CRM data.", _ => "An unexpected server error occurred." };
    context.Response.StatusCode = status; context.Response.ContentType = "application/problem+json";
    if (exception is not null) context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("MiniCRM.Errors").LogError(exception, "Request failed. Trace {TraceId}", context.TraceIdentifier);
    await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = status, Title = title, Extensions = { ["traceId"] = context.TraceIdentifier } });
}));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("frontend"); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapHealthChecks("/health"); app.MapControllers().RequireRateLimiting("api");
if (!app.Environment.IsEnvironment("Testing")) await IdentityBootstrap.InitializeAsync(app.Services, app.Configuration, app.Environment);
app.Run();

public partial class Program { }
