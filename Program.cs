using ChessEngine.Interfaces;
using ChessEngine.Models;
using ChessEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC controllers with views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Register services
builder.Services.AddScoped<Board>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddSingleton<IPositionEvaluator, PositionEvaluator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Board}/{action=Play}/{id?}");

app.Run();
