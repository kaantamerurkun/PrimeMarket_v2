using Microsoft.EntityFrameworkCore;
using PrimeMarket.Data;
using PrimeMarket.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Microsoft.Build.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add email settings configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Add controllers with views and better JSON handling
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add session services with longer timeout
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Set session timeout to 60 minutes
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add CORS if needed for API calls
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("https://localhost:7245")
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/404");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use CORS if needed
app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

// Enable session - make sure this comes before endpoints
app.UseSession();

// Fix for CS0136: Renamed the inner 'scope' variable to 'innerScope' to avoid name conflict.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    using (var innerScope = app.Services.CreateScope()) // Renamed 'scope' to 'innerScope'
    {
        var innerServices = innerScope.ServiceProvider;
        try
        {
            var context = innerServices.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created (this will apply migrations if needed)
            context.Database.EnsureCreated();

            // Reset database tables Not: SAKIN BURAYI YORUMDAN ÇIKARTMA!
            //context.Database.ExecuteSqlRaw("EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
            //context.Database.ExecuteSqlRaw("EXEC sp_MSForEachTable 'DELETE FROM ?'");
            //context.Database.ExecuteSqlRaw("EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
            //context.Database.ExecuteSqlRaw("EXEC sp_MSForEachTable 'IF OBJECTPROPERTY(OBJECT_ID(''?''), ''TableHasIdentity'') = 1 DBCC CHECKIDENT (''?'', RESEED, 0)'");
        }
        catch (Exception ex)
        {
            var logger = innerServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }
}

// Configure routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add admin-specific routes if needed
app.MapControllerRoute(
    name: "admin",
    pattern: "Admin/{controller=Admin}/{action=AdminLogin}/{id?}");

app.Run();