var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Makes the entry point visible to test projects.
public partial class Program { }
