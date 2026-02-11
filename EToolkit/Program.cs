using EToolkit.Application;
using EToolkit.Infrastructure;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IRecordFilteringService, RecordFilteringService>();
builder.Services.AddScoped<IRecordImportUseCase, RecordImportUseCase>();
builder.Services.AddScoped<RecordImportService>();
builder.Services.AddScoped<ICsvRecordImporter, CsvRecordImporter>();
builder.Services.AddScoped<RecordFilterPreviewService>();
builder.Services.AddSingleton<FootprintNormalizer>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();