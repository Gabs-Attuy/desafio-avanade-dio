using AuthService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AuthContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
        
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider.GetRequiredService<AuthContext>();

    var configuration =
        scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await DbInitializer.SeedAsync(
        context,
        configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();