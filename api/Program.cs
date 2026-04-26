using EToolkit.Application;
using EToolkit.Application.Logger;
using EToolkit.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Observeability
builder.Services.AddSingleton<IAnnotatedRowLogPolicy, AnnotatedRowLogPolicy>();
builder.Services.AddSingleton<IAnnotatedRowLogger, AnnotatedRowLogger>();

builder.Services.AddScoped<IRecordIssueCollector, RecordIssueCollector>();
builder.Services.AddScoped<IRecordFilteringService, RecordFilteringService>();
builder.Services.AddScoped<IRecordImportService, RecordImportService>();
builder.Services.AddScoped<ICsvRecordImporter, CsvRecordImporter>();
builder.Services.AddScoped<IRecordExportService, RecordExportService>();
builder.Services.AddScoped<RecordFilterPreviewService>();
builder.Services.AddScoped<IRecordWriterService<CsvComponentPlacementRow>, CsvRecordExporter>();
builder.Services.AddSingleton<IFootprintNormalizer, FootprintNormalizer>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCors("Frontend");

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();