using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCRM.Application;
using MiniCRM.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); builder.Logging.AddJsonConsole();
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".keys")));
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
builder.Services.AddDbContext<CrmDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddIdentity<CrmUser, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 12; options.Password.RequireDigit = true; options.Password.RequireUppercase = true; options.Password.RequireLowercase = true; options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5; options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<CrmDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => { options.Cookie.Name = "MiniCRM.Mvc"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; options.LoginPath = "/Account/Login"; options.AccessDeniedPath = "/Account/Denied"; options.SlidingExpiration = true; });
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>(); builder.Services.AddScoped<ICrmService, SqlCrmService>();
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql-server");

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.MapHealthChecks("/health");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
await IdentityBootstrap.InitializeAsync(app.Services, app.Configuration, app.Environment);
app.Run();
