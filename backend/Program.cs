using IncidentCommander.Api.Data;
using IncidentCommander.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<MetaModelOptions>(builder.Configuration.GetSection(MetaModelOptions.SectionName));
// Env var wins over appsettings so the key never has to live in a file.
builder.Services.PostConfigure<MetaModelOptions>(options =>
{
    var envKey = Environment.GetEnvironmentVariable("META_MODEL_API_KEY");
    if (!string.IsNullOrWhiteSpace(envKey))
    {
        options.ApiKey = envKey;
    }
});

builder.Services.AddHttpClient<IMetaModelClient, MetaModelClient>();
builder.Services.AddHttpClient<IVisionModelClient, MetaVisionClient>();
builder.Services.AddHttpClient<IVoiceModelClient, MetaVoiceClient>();
builder.Services.AddSingleton<IncidentPlanVerifier>();
builder.Services.AddSingleton<EvidenceStorage>();
builder.Services.AddScoped<AuditLogger>();

var dbPath = builder.Configuration.GetConnectionString("Default") ?? "Data Source=incidents.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(dbPath));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
