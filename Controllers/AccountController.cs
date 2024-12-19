using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyChat.Models;
using MyChat.ViewModels;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace MyChat.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly MyChatContext _context;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, MyChatContext context, IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailService = emailService;
    }
    [Authorize (Roles = "admin")]
    public IActionResult Index()
    {
        List<User> users = _userManager.Users.ToList();
        return View(users);
    }
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            User user = await _userManager.FindByEmailAsync(model.Identifier) ?? await _userManager.FindByNameAsync(model.Identifier);
        
            if (user != null)
            {
                SignInResult result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction("Index", "Chat");
                }
            }
            
            ModelState.AddModelError("", "Неверный логин или пароль!");
        }

        return View(model);
    }
    
    [HttpGet]
    public IActionResult RegisterUser()
    {
        return View();
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterUser(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUserEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingUserEmail != null)
        {
            ViewBag.ErrorMessage = "Ошибка: Этот адрес электронной почты уже используется другим пользователем!";
            return View(model);
        }
    
        var existingUserName = await _userManager.FindByNameAsync(model.UserName);
        if (existingUserName != null)
        {
            ViewBag.ErrorMessage = "Ошибка: Этот логин уже используется другим пользователем!";
            return View(model);
        }
        
        var currentDate = DateTime.UtcNow;
        var userAge = currentDate.Year - model.DateOfBirth.Year;
        if (model.DateOfBirth > currentDate.AddYears(-userAge)) 
        {
            userAge--;
        }
        if (userAge < 18)
        {
            ViewBag.ErrorMessage = "Ошибка: Нельзя зарегистрироваться пользователям моложе 18 лет!";
            return View(model);
        }
        
        var user = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            Avatar = model.Avatar,
            DateOfBirth = model.DateOfBirth.ToUniversalTime()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "user");
            return RedirectToAction("Index", "Account");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUserEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserEmail != null)
            {
                ViewBag.ErrorMessage = "Ошибка: Этот адрес электронной почты уже используется другим пользователем!";
                return View(model);
            }

            var existingUserName = await _userManager.FindByNameAsync(model.UserName);
            if (existingUserName != null)
            {
                ViewBag.ErrorMessage = "Ошибка: Этот логин уже используется другим пользователем!";
                return View(model);
            }

            var currentDate = DateTime.UtcNow;
            var userAge = currentDate.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth > currentDate.AddYears(-userAge))
            {
                userAge--;
            }
            if (userAge < 18)
            {
                ViewBag.ErrorMessage = "Ошибка: Нельзя зарегистрироваться пользователям моложе 18 лет!";
                return View(model);
            }

            User user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                Avatar = model.Avatar,
                DateOfBirth = model.DateOfBirth.ToUniversalTime()
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "user");

                try
                {
                    var subject = "Добро пожаловать в наш чат!";
                    var body = $@"
                    <h1>Добро пожаловать, {model.UserName}!</h1>
                    <p>Благодарим за регистрацию в нашем сервисе. Теперь вы можете общаться в чате и наслаждаться всеми функциями нашей платформы.</p>
                    <p>Ваш логин: {model.UserName}</p>
                    <p>Если у вас есть вопросы, свяжитесь с нами по адресу поддержки.</p>";

                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    Console.WriteLine($"Email отправлен на адрес {user.Email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отправке email: {ex.Message}");
                }

                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Chat");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }


    [Authorize]
    public async Task<IActionResult> Profile(int? userid)
    {
        User user = await _userManager.GetUserAsync(User);
        if (userid == null)
        {
            userid = user.Id;
        }
        
        var existingUser =  _context.Users.FirstOrDefault(u => u.Id == userid);
        if (existingUser == null)
        {
            return NotFound($"Пользователь не найден.");
        }

        if (existingUser != null)
        {
            var messages = _context.Messages.Where(m => m.UserId == existingUser.Id).ToList();
            ViewBag.Messages = messages.Count;
            ViewBag.CurrentUser = user.Id;
            return View(existingUser);
        }
        return NotFound();
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit()
    {
        User user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("admin"))
        {
            return RedirectToAction("Profile", "Account");
        }
        var model = new EditViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            Avatar = user.Avatar,
            DateOfBirth = user.DateOfBirth
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        if (ModelState.IsValid)
        {
            User user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("admin"))
            {
                return RedirectToAction("Profile", "Account");
            }

            if (user != null)
            {
                var existingUserEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserEmail != null && existingUserEmail.Id != user.Id)
                {
                    ViewBag.ErrorMessage = "Ошибка: Этот адрес электронной почты уже используется другим пользователем!";
                    return View(model);
                }

                var existingUserName = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserName != null && existingUserName.Id != user.Id)
                {
                    ViewBag.ErrorMessage = "Ошибка: Этот логин уже используется другим пользователем!";
                    return View(model);
                }

                var currentDate = DateTime.UtcNow;
                var userAge = currentDate.Year - model.DateOfBirth.Year;
                if (model.DateOfBirth > currentDate.AddYears(-userAge))
                {
                    userAge--;
                }
                if (userAge < 18)
                {
                    ViewBag.ErrorMessage = "Ошибка: Нельзя зарегистрироваться пользователям моложе 18 лет!";
                    return View(model);
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.Avatar = model.Avatar;
                user.DateOfBirth = DateTime.SpecifyKind(model.DateOfBirth, DateTimeKind.Utc);

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    string subject = "Ваш профиль был обновлён";
                    string message = $"Здравствуйте, {user.UserName}!\n\nВаши данные на нашем сайте были успешно обновлены.\n\nЕсли вы не совершали этих действий, пожалуйста, свяжитесь с нами.";

                    try
                    {
                        await _emailService.SendEmailAsync(user.Email, subject, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при отправке email: {ex.Message}");
                    }

                    return RedirectToAction("Profile", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        return View(model);
    }


    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
    public async Task<IActionResult> BlockUser(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound($"Пользователь с ID {userId} не найден.");
        }
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("admin"))
        {
            return RedirectToAction("Index", "Account");
        }
        user.LockoutEnd = DateTimeOffset.MaxValue;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Account");
        }
        return RedirectToAction("Index", "Account");
    }

    public async Task<IActionResult> UnblockUser(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound($"Пользователь с ID {userId} не найден.");
        }
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("admin"))
        {
            return RedirectToAction("Index", "Account");
        }

        if (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now)
        {
            return RedirectToAction("Index", "Account");
        }
        user.LockoutEnd = null;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Account");
        }
        return RedirectToAction("Index", "Account");
    }
    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> UserEdit(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        var model = new UserEditViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            Avatar = user.Avatar
        };

        return View(model);
    }
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> UserEdit(int userId, UserEditViewModel uevm)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            
            var existingEmailUser = await _userManager.FindByEmailAsync(uevm.Email);
            if (existingEmailUser != null && existingEmailUser.Id != user.Id)
            {
                ViewBag.ErrorMessage = "Этот адрес электронной почты уже используется другим пользователем!";
                return View(uevm);
            }

            var existingUserNameUser = await _userManager.FindByNameAsync(uevm.UserName);
            if (existingUserNameUser != null && existingUserNameUser.Id != user.Id)
            {
                ViewBag.ErrorMessage = "Этот логин уже используется другим пользователем!";
                return View(uevm);
            }

            user.UserName = uevm.UserName;
            user.Email = uevm.Email;
            user.Avatar = uevm.Avatar;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Account");
            }
        }

        return View(uevm);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SendMessageCountEmail()
    {
        try
        {
            Console.WriteLine("[DEBUG] Начало выполнения метода SendMessageCountEmail");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("[DEBUG] Пользователь не найден.");
                return BadRequest("Пользователь не найден.");
            }

            Console.WriteLine($"[DEBUG] Пользователь найден: {user.UserName}, Email: {user.Email}");

            int messageCount = _context.Messages.Count(m => m.UserId == user.Id);
            Console.WriteLine($"[DEBUG] Количество сообщений пользователя: {messageCount}");

            string subject = "Количество отправленных сообщений";
            string message = $"Здравствуйте, {user.UserName}!\n\nВы отправили {messageCount} сообщений на нашем сайте.\n\nСпасибо, что пользуетесь нашим сервисом!";
            Console.WriteLine("[DEBUG] Сформировано письмо для отправки.");

            await _emailService.SendEmailAsync(user.Email, subject, message);
            Console.WriteLine($"[DEBUG] Email успешно отправлен на адрес {user.Email}.");

            return Ok(new { success = true, message = "Email успешно отправлен." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Ошибка при отправке email: {ex.Message}");
            return StatusCode(500, "Произошла ошибка при отправке email.");
        }
    }


}