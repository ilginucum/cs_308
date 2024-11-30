using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using e_commerce.Models;
using e_commerce.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

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
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLogin model, string returnUrl = null)
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
                        new Claim(ClaimTypes.Role, user.UserType.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    // Guest cart birleştirme işlemini ekledik
                    var cartController = new ShoppingCartController(
                        HttpContext.RequestServices.GetRequiredService<IMongoDBRepository<ShoppingCart>>(),
                        HttpContext.RequestServices.GetRequiredService<IMongoDBRepository<Product>>(),
                        HttpContext.RequestServices.GetRequiredService<ILogger<ShoppingCartController>>());
                    
                    cartController.ControllerContext = new ControllerContext
                    {
                        HttpContext = HttpContext
                    };
                    
                    await cartController.MergeGuestCart(user.Id);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            ViewData["ReturnUrl"] = returnUrl;
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
        try
        {
            var existingUser = await _userRepository.FindOneAsync(u => u.Username == model.Username || u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username or email already exists.");
                return View(model);
            }

            // Ensure UserType is Customer regardless of what was submitted
            model.UserType = UserType.Customer;
            model.Password = HashPassword(model.Password);

            await _userRepository.InsertOneAsync(model);

            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during user registration: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
        }
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

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Username = model.Username;

            try
            {
                await _userRepository.ReplaceOneAsync(user);
                TempData["SuccessMessage"] = "Profile updated successfully.";
                _logger.LogInformation($"User profile updated successfully: {user.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating user profile: {user.Id}");
                ModelState.AddModelError("", "An error occurred while updating the profile. Please try again.");
                return View("Profile", model);
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}