using EToolkit.Application;
using EToolkit.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IRecordFilteringService, RecordFilteringService>();
builder.Services.AddScoped<IRecordImportUseCase, RecordImportUseCase>();
builder.Services.AddScoped<IRecordImportService, RecordImportService>();
builder.Services.AddScoped<ICsvRecordImporter, CsvRecordImporter>();
builder.Services.AddScoped<RecordExportService>();
builder.Services.AddScoped<RecordFilterPreviewService>();
builder.Services.AddScoped<IRecordExportService<CsvComponentPlacementRow>, CsvRecordExporter>();
builder.Services.AddSingleton<FootprintNormalizer>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();