using LabAssistanteProject.Data;
using LabAssistanteProject.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------- SERVICES ----------------
// Add MVC
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = "re_j4fBHG8A_Pi2DwTr7WeyU6atomWXH8Nro"; // Use User Secrets or AppSettings for this
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddControllersWithViews();
// Add DbContext
builder.Services.AddDbContext<MyAppContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionString")
    )
);

// Add JWT helper service
builder.Services.AddScoped<JwtService>();

// ---------------- JWT AUTHENTICATION ----------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Token validation
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
        ValidAudience = builder.Configuration["Jwt:Audience"]!,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        )
    };

    // Read JWT from cookie - YEHI WOH IMPORTANT PART HAI!
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["jwt"];
            return Task.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// ---------------- BUILD APP ----------------
var app = builder.Build();

// Error handling & HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ Middleware order is important
app.UseAuthentication();   
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();