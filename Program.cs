using ConsultasSQL.Logic;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IBpcsConnectionFactory, BpcsConnectionFactory>();
builder.Services.AddTransient<BPCS>();
var app = builder.Build();

var reporte = BpcsConnectivityDiagnostics.RunAll();
Console.WriteLine(reporte);
// o regístralo con ILogger / devuélvelo por un endpoint GET temporal

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
