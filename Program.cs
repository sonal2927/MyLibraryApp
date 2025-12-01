using LibraryManagementSystem.Models;
using LibraryManagementSystem.Data; // ✅ For DbSeeder
using Microsoft.EntityFrameworkCore;
using MyLibrary.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ Database Context
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("LibraryConnection"),
        new MySqlServerVersion(new Version(8, 0, 34))
    ));

// ✅ Middleware Services
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(); // ✅ Enables session
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LibraryDbContext>();

    // Optional: Apply migrations (you can remove this if you already migrated manually)
    context.Database.Migrate();

    // ✅ Call seeding method
    DbSeeder.Seed(context);
}

// ✅ Error Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();         // ✅ Enables session
app.UseAuthorization();

// ✅ Default MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
