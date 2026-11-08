using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Mizan.Api.Endpoints;
using Mizan.Api.Infrastructure;
using Mizan.Application;
using Mizan.Infrastructure;
using Mizan.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Convenience for local development only. Production runs migrations as a deploy step.
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MizanDbContext>().Database.MigrateAsync();
}

// Insights, notifications and error messages follow the app's language (Accept-Language), Arabic by default.
// Only the UI language changes: number and date formatting stay invariant (no Arabic separators or Hijri dates).
app.UseRequestLocalization(options =>
{
    options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, new CultureInfo("ar"));
    options.SupportedCultures = [CultureInfo.InvariantCulture];
    options.SupportedUICultures = [new CultureInfo("ar"), new CultureInfo("en")];
});

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapCategoryEndpoints();
app.MapBudgetEndpoints();
app.MapTransactionEndpoints();
app.MapGoalEndpoints();
app.MapNotificationEndpoints();
app.MapDashboardEndpoints();
app.MapBankingEndpoints();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program;
