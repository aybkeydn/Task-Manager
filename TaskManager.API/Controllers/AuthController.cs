using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TaskManager.API.DTOs;
using TaskManager.API.Services;
using TaskManager.API.Models;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        //Bu kısımda controller'ın ihtiyaç duyduğu servisler constructor üzerinden enjekte edilir
        private readonly IAuthService _authService;// Kullanıcı kayıt ve doğrulama işlemlerini yönetir
        private readonly IConfiguration _configuration;// appsettings.json gibi yapılandırma dosyalarına erişim sağlar(jwt için)

        public AuthController(IAuthService authService, IConfiguration configuration) // servislerin enjekte edildiği constructor
        {
            _authService = authService;// servislerin const. üzerinden atanması
            _configuration = configuration;//aynı işlem 
        }

        [HttpPost("register")] // kullanıcı kayıt işlemi 

        // bir kullanıcı kaydı isteğini işler.
        // HTTP isteğinden gelen kullanıcı kayıt verilerini (userRegistration) alır, asenkron olarak işler
        // ve bir HTTP yanıtı (IActionResult) döndürür.
        // Asenkron yapı sayesinde, kullanıcı kaydı işlemi tamamlanana kadar
        // uygulama diğer istekleri işlemeye devam edebilir
        public async Task <IActionResult> Register(UserRegistrationDto userRegistration) // IActionResult Controller'dan döndürülebilecek Ok(), BadRequest(), NotFound() gibi sonuçların hepsi bu arayüzü uygular.
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterUserAsync(userRegistration);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            return Ok(new { Message = "Kullanıcı başarıyla oluşturuldu", UserId = result.UserId });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto userLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _authService.ValidateUserAsync(userLogin);
            if (user == null)
            {
                return Unauthorized(new { Message = "Geçersiz kullanıcı adı veya şifre" });
            }

            //kullanıcı başarıyla giriş yaptığında çalışır ve
            //istemciye (örneğin, web tarayıcısı) kullanıcının kimlik bilgilerini
            //ve sonraki isteklerde kullanabileceği bir token gönderir.
            //İstemci bu token'ı "Authorization"başlığında saklayarak,
            //korumalı api endpoint'lerine erişmek için kullanır.
            var token = GenerateJwtToken(user);//jwt token oluşturur
            return Ok(new { Token = token, UserId = user.UserId, Username = user.Username }); // token ve kullanıcı bilgilerini döndürür
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] // kullanıcı bilgilerinden "claims" (iddialar) oluşturulur (kullanıcı kimliğini taşıyan bilgiler)
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            //token'ın geçerlilik süresi,yayıncısı ve hedef kitlesi belirlenir.
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);//oluşturulan token string olarak döndürülür.
        }
    }
}