using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Cryptography;
using System.Text;

namespace e_commerce.Controllers
{
    public class UserController : Controller
    {
        private readonly IMongoDBRepository<UserRegistration> _userRepository;
       private readonly ILogger<UserController> _logger;

       public UserController(IMongoDBRepository<UserRegistration> userRepository, ILogger<UserController> logger)
       {
           _userRepository = userRepository;
           _logger = logger;
       }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLogin model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.FindOneAsync(u => u.Username == model.Username);
                if (user != null && VerifyPassword(model.Password, user.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.UserType.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

                [HttpPost]
        public async Task<IActionResult> Registration(UserRegistration model)
        {
            //if (ModelState.IsValid)
            //{
                try
                {
                    // Check if the user already exists
                    var existingUser = await _userRepository.FindOneAsync(u => u.Username == model.Username || u.Email == model.Email);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError(string.Empty, "Username or email already exists.");
                        return View(model);
                    }

                    // Hash the password
                    model.Password = HashPassword(model.Password);
                     // Assign the role based on UserType
                    string role = model.UserType switch
                    {
                        UserType.ProductManager => "ProductManager",
                        UserType.SalesManager => "SalesManager",
                        UserType.Customer => "Customer",
                        _ => throw new ArgumentException("Invalid UserType")
                    };


                    // Create a new user object with all the required fields, including UserType
                    var newUser = new UserRegistration
                    {
                        FullName = model.FullName,
                        Username = model.Username,
                        Email = model.Email,
                        Password = model.Password,
                        PhoneNumber = model.PhoneNumber,
                        UserType = model.UserType  // Add this line to include UserType
                    };

                    // Insert the user into MongoDB
                    await _userRepository.InsertOneAsync(newUser);
                        // Log the user in...
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, newUser.Username),
                        new Claim(ClaimTypes.Role, role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
            

                    TempData["SuccessMessage"] = "Registration successful. Please log in.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during user registration: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                }
            //}

            return View(model);
        }




        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }
    }
}