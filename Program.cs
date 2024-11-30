using Microsoft.AspNetCore.Authentication.Cookies;
using e_commerce.Data;
using e_commerce.Models;
using e_commerce.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

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

builder.Services.AddScoped<IMongoDBRepository<ProductComment>>(sp =>
   new MongoDBRepository<ProductComment>(
       sp.GetRequiredService<IConfiguration>(),
       "ProductComments",
       sp.GetRequiredService<ILogger<MongoDBRepository<ProductComment>>>()
   ));

builder.Services.AddScoped<IMongoDBRepository<Rating>>(sp =>
   new MongoDBRepository<Rating>(
       sp.GetRequiredService<IConfiguration>(),
       "Ratings",
       sp.GetRequiredService<ILogger<MongoDBRepository<Rating>>>()
   ));

builder.Services.AddScoped<IMongoDBRepository<CreditCard>>(sp =>
   new MongoDBRepository<CreditCard>(
       sp.GetRequiredService<IConfiguration>(),
       "CreditCard",
       sp.GetRequiredService<ILogger<MongoDBRepository<CreditCard>>>()
   ));

builder.Services.AddScoped<IMongoDBRepository<ShoppingCart>>(sp =>
   new MongoDBRepository<ShoppingCart>(
       sp.GetRequiredService<IConfiguration>(),
       "ShoppingCarts",
       sp.GetRequiredService<ILogger<MongoDBRepository<ShoppingCart>>>()
   ));

builder.Services.AddScoped<IMongoDBRepository<Address>>(sp =>
   new MongoDBRepository<Address>(
       sp.GetRequiredService<IConfiguration>(),
       "Address",
       sp.GetRequiredService<ILogger<MongoDBRepository<Address>>>()
   ));

builder.Services.AddScoped<IMongoDBRepository<Order>>(sp =>
   new MongoDBRepository<Order>(
       sp.GetRequiredService<IConfiguration>(),
       "Orders",
       sp.GetRequiredService<ILogger<MongoDBRepository<Order>>>()
   ));

// Add session support
builder.Services.AddSession(options =>
{
   options.IdleTimeout = TimeSpan.FromMinutes(30); // Session süresini 7 güne çıkardım
   options.Cookie.HttpOnly = true;
   options.Cookie.IsEssential = true;
});

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
   .AddCookie(options =>
   {
       options.LoginPath = "/User/Login";
       options.LogoutPath = "/User/Logout";
       options.AccessDeniedPath = "/User/AccessDenied";
   });
   
// Add this line with your other service registrations
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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
app.UseSession(); // Session middleware'ini ekledik
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
   name: "default",
   pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();