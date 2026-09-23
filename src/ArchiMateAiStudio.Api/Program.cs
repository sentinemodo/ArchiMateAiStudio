using ArchiMateAiStudio.Application;
using ArchiMateAiStudio.Infrastructure;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (ArchiMateAiStudio.Infrastructure.DependencyInjection.ResolveConnectionString(builder.Configuration) is not null)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StudioDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
