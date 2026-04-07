using EToolkit.Application;
using EToolkit.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IRecordFilteringService, RecordFilteringService>();
builder.Services.AddScoped<IRecordImportService, RecordImportService>();
builder.Services.AddScoped<IRecordImportService, RecordImportService>();
builder.Services.AddScoped<ICsvRecordImporter, CsvRecordImporter>();
builder.Services.AddScoped<IRecordExportService, RecordExportService>();
builder.Services.AddScoped<RecordFilterPreviewService>();
builder.Services.AddScoped<IRecordWriterService<CsvComponentPlacementRow>, CsvRecordExporter>();
builder.Services.AddSingleton<IFootprintNormalizer, FootprintNormalizer>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();