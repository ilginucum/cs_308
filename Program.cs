using Microsoft.AspNetCore.Authentication.Cookies;
using e_commerce.Data;
using e_commerce.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register MongoDB Repository with the correct database and collection name
builder.Services.AddScoped(typeof(IMongoDBRepository<>), typeof(MongoDBRepository<>));
builder.Services.AddScoped<IMongoDBRepository<UserRegistration>>(sp => 
    new MongoDBRepository<UserRegistration>(sp.GetRequiredService<IConfiguration>(), "LoginInfo"));

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Login";
        options.LogoutPath = "/User/Logout";
    });

var app = builder.Build();

// Testing MongoDB connection
//using (var scope = app.Services.CreateScope())
//{
   // var mongoRepo = scope.ServiceProvider.GetRequiredService<IMongoDBRepository<UserRegistration>>();
    //try
    //{
        //var newUser = new UserRegistration
        //{
           // Username = "TestUser",
            //Email = "test@example.com",
            //Password = "password123"
        //};
        //await mongoRepo.InsertOneAsync(newUser);
        //Console.WriteLine("MongoDB connection successful. Test user inserted.");
   // }
    //catch (Exception ex)
    //{
       // Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
    //}
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
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
