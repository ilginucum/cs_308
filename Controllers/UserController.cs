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
using e_commerce.DTO;

namespace e_commerce.Controllers
{
    public class UserController : Controller
    {
        private readonly IMongoDBRepository<UserRegistration> _userRepository;
        private readonly IMongoDBRepository<ProfileAddress> _addressRepository;
        private readonly ILogger<UserController> _logger;

        public UserController(IMongoDBRepository<UserRegistration> userRepository, IMongoDBRepository<ProfileAddress> addressRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _addressRepository = addressRepository;
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

                    // Handle guest cart merging
                    var cartController = new ShoppingCartController(
                        HttpContext.RequestServices.GetRequiredService<IMongoDBRepository<ShoppingCart>>(),
                        HttpContext.RequestServices.GetRequiredService<IMongoDBRepository<Product>>(),
                        HttpContext.RequestServices.GetRequiredService<ILogger<ShoppingCartController>>(),
                        HttpContext.RequestServices.GetRequiredService<IMongoDBRepository<WishlistItem>>() 
                    );


                    await cartController.MergeGuestCart(user.Id);

                    // Role-based redirect logic
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    // Redirect based on user role
                    return user.UserType switch
                    {
                        UserType.SalesManager => RedirectToAction("Index", "Sales"),
                        UserType.ProductManager => RedirectToAction("Index", "Product"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateManagerAccount(UserRegistration model)
        {
            try
            {
                var existingUser = await _userRepository.FindOneAsync(u => 
                    u.Username == model.Username || u.Email == model.Email);
                    
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Username or email already exists.");
                    return View(model);
                }

                // Ensure UserType is either SalesManager or ProductManager
                if (model.UserType != UserType.SalesManager && model.UserType != UserType.ProductManager)
                {
                    ModelState.AddModelError(string.Empty, "Invalid user type for manager account.");
                    return View(model);
                }

                model.Password = HashPassword(model.Password);
                await _userRepository.InsertOneAsync(model);

                _logger.LogInformation($"Manager account created successfully: {model.Username}");
                TempData["SuccessMessage"] = "Manager account created successfully.";
                return RedirectToAction("ManagerAccounts", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating manager account: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the account.");
                return View(model);
            }
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

        // Get user's addresses using FilterByAsync instead of FilterAsync
        var addresses = await _addressRepository.FilterByAsync(a => a.UserId == userId);

        var model = new UserProfileViewModel
        {
            Id = user.Id,  // Include the MongoDB ObjectId
            FullName = user.FullName,
            Email = user.Email,
            Username = user.Username,
            PhoneNumber = user.PhoneNumber,
            Addresses = addresses.ToList()
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

    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userRepository.FindByIdAsync(userId);
        
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        // Don't update Username as it should remain unchanged
        
        await _userRepository.ReplaceOneAsync(user);
        
        TempData["SuccessMessage"] = "Profile updated successfully.";
        _logger.LogInformation($"User profile updated successfully: {userId}");
        
        return RedirectToAction(nameof(Profile));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while updating user profile");
        ModelState.AddModelError("", "An error occurred while updating the profile. Please try again.");
        return View("Profile", model);
    }
}

[HttpPost]
[Authorize(Roles = "Customer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddAddress([FromBody] AddressDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(new { errors = ModelState });
    }

    try
    {
        // Log the received data for debugging
        _logger.LogInformation($"Received AddressDto: {System.Text.Json.JsonSerializer.Serialize(dto)}");

        var address = new ProfileAddress
        {
            StreetAddress = dto.StreetAddress,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        // Log the created address object
        _logger.LogInformation($"Created ProfileAddress: {System.Text.Json.JsonSerializer.Serialize(address)}");

        await _addressRepository.InsertOneAsync(address);
        
        TempData["SuccessMessage"] = "Address added successfully.";
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while adding address");
        return StatusCode(500, new { message = "An error occurred while adding the address." });
    }
}

[HttpPost]
[Authorize(Roles = "Customer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateAddress([FromBody] UpdateAddressDto dto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(new { errors = ModelState });
    }

    try
    {
        // Log the received data for debugging
        _logger.LogInformation($"Received UpdateAddressDto: {System.Text.Json.JsonSerializer.Serialize(dto)}");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingAddress = await _addressRepository.FindOneAsync(a => 
            a.Id == dto.Id && a.UserId == userId);

        if (existingAddress == null)
        {
            return NotFound(new { message = "Address not found" });
        }

        existingAddress.StreetAddress = dto.StreetAddress;
        existingAddress.City = dto.City;
        existingAddress.State = dto.State;
        existingAddress.ZipCode = dto.ZipCode;
        // UserId remains unchanged

        // Log the updated address object
        _logger.LogInformation($"Updating address to: {System.Text.Json.JsonSerializer.Serialize(existingAddress)}");

        await _addressRepository.ReplaceOneAsync(existingAddress);
        
        TempData["SuccessMessage"] = "Address updated successfully.";
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while updating address: {dto.Id}");
        return StatusCode(500, new { message = "An error occurred while updating the address." });
    }
}

[HttpPost]
[Authorize(Roles = "Customer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAddress(string id)
{
    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var address = await _addressRepository.FindOneAsync(a => 
            a.Id == id && a.UserId == userId);

        if (address == null)
        {
            return NotFound(new { message = "Address not found" });
        }

        await _addressRepository.DeleteOneAsync(id);
        
        TempData["SuccessMessage"] = "Address deleted successfully.";
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while deleting address: {id}");
        return StatusCode(500, new { message = "An error occurred while deleting the address." });
    }
}

[HttpGet]
[Authorize]
public async Task<IActionResult> GetAddress(string id)
{
    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var address = await _addressRepository.FindOneAsync(a => 
            a.Id == id && a.UserId == userId);

        if (address == null)
        {
            _logger.LogWarning($"Address not found for ID: {id} and UserID: {userId}");
            return NotFound(new { message = "Address not found" });
        }

        _logger.LogInformation($"Successfully retrieved address for ID: {id}");
        return Json(new
        {
            streetAddress = address.StreetAddress,
            city = address.City,
            state = address.State,
            zipCode = address.ZipCode
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while getting address: {id}");
        return StatusCode(500, new { message = "An error occurred while getting the address." });
    }
}

[HttpGet]
[Authorize]
public async Task<IActionResult> GetProfileInfo()
{
    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userRepository.FindByIdAsync(userId);
        
        if (user == null)
        {
            _logger.LogWarning($"User not found for ID: {userId}");
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation($"Successfully retrieved profile info for user: {userId}");
        return Json(new
        {
            fullName = user.FullName,
            email = user.Email,
            phoneNumber = user.PhoneNumber
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching profile information");
        return StatusCode(500, new { message = "Error fetching profile information" });
    }
}
    }
}