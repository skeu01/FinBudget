using Application.Auth;
using Infrastructure.Authentication;
using Infrastructure.Ocr;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Contexts;
using Presentation.Middleware;
using Presentation.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Configure the PostgreSQL connection string 'DefaultConnection'.");
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddPostgresPersistence(connectionString);
builder.Services.AddHttpClient("ReceiptOcrAzure", client =>
    client.Timeout = TimeSpan.FromSeconds(100));
builder.Services.AddReceiptOcr(builder.Configuration);
builder.Services.AddScoped<IFinancialApiService, FinancialApiService>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in new[]
             {
                 Permissions.ProfileRead,
                 Permissions.HouseholdsRead,
                 Permissions.HouseholdsManage,
                 Permissions.TransactionsRead,
                 Permissions.TransactionsManage,
                 Permissions.ReportsExport,
                 Permissions.UsersManage
             })
    {
        options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
    }

    options.AddPolicy("Administrator", policy => policy.RequireRole("Admin"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.Run();
