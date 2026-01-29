using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

string? connectionString = builder.Configuration.GetConnectionString("ConnectionString");

if (connectionString is null)
{
    throw new InvalidOperationException("Connection string 'ConnectionString' not found.");
}
builder.Services.AddDbContext<NKZAPI.Data.NKZAPIContext>(options =>
    options.UseNpgsql(connectionString));



// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>{ 
        c.DocumentTitle = "API NKZ - V1";
        c.SwaggerEndpoint("/swagger/v1/swagger.json","API NKZ - V1");
    });

}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
