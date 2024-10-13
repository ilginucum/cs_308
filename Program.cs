using Microsoft.AspNetCore.Authentication.Cookies;
using e_commerce.Data;
using e_commerce.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Register MongoDB Repository with the correct database and collection name
builder.Services.AddScoped(typeof(IMongoDBRepository<>), typeof(MongoDBRepository<>));
builder.Services.AddScoped<IMongoDBRepository<UserRegistration>>(sp =>
    new MongoDBRepository<UserRegistration>(
        sp.GetRequiredService<IConfiguration>(), 
        "LoginInfo",
        sp.GetRequiredService<ILogger<MongoDBRepository<UserRegistration>>>()
    ));
builder.Services.AddScoped<IMongoDBRepository<Product>>(sp =>
    new MongoDBRepository<Product>(
        sp.GetRequiredService<IConfiguration>(), 
        "Products",
        sp.GetRequiredService<ILogger<MongoDBRepository<Product>>>()
    ));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();