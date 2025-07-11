using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Models.Requests;
using ElevatorApp.DataAccess.Entities;
using ElevatorApp.DataAccess.Context;

namespace ElevatorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ElevatorDbContext _db;

        public UserController(ElevatorDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (!IsValidEmail(request.Email))
                return BadRequest("Invalid email.");

            if (!IsValidPassword(request.Password))
                return BadRequest("Password must be at least 6 characters, with a letter and a digit.");

            if (request.Password != request.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            if (_db.Users.Any(u => u.Email == request.Email))
                return Conflict("User already exists.");

            var user = new User
            {
                Email = request.Email,
                Password = request.Password // בעתיד: להצפין
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok("User registered successfully.");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return false;

            bool hasLetter = password.Any(char.IsLetter);
            bool hasDigit = password.Any(char.IsDigit);
            return hasLetter && hasDigit;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null || user.Password != request.Password)
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(new { id = user.Id, email = user.Email });
        }

    }
}
