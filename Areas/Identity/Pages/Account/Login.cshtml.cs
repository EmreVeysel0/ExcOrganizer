// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ExcOrganizer.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ExcOrganizer.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        // КОРЕКЦИЯ: Добавяме UserManager в конструктора, за да можем да търсим по имейл
        public LoginModel(SignInManager<IdentityUser> signInManager,
                          ILogger<LoginModel> logger,
                          UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Моля, въведете потребителско име или имейл.")]
            [Display(Name = "Потребителско име или имейл")]
            public string Username { get; set; } // Вече се казва Username

            [Required(ErrorMessage = "Моля, въведете парола.")]
            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string Password { get; set; }

            [Display(Name = "Запомни ме")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Първо опитваме вход директно с написаното (като потребителско име)
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                // 2. Ако не успее, проверяваме дали това не е имейл
                if (!result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(Input.Username);
                    if (user != null)
                    {
                        // Ако намерим потребител с такъв имейл, влизаме с неговия UserName
                        result = await _signInManager.PasswordSignInAsync(
                            user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    }
                }

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Невалиден вход. Проверете данните си.");
                    return Page();
                }
            }

            return Page();
        }
    }
}