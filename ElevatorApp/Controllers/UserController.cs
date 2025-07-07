using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Models;
using ElevatorApp.Services;

namespace ElevatorApp.Controllers
{
    /// <summary>
    /// Controller for handling user-related operations such as registration and login.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="userService">The service responsible for user operations.</param>
        public UserController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        /// <param name="request">The user registration request containing email and password.</param>
        /// <returns>
        /// A success response with the newly created user's ID and email.
        /// </returns>
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            var user = _userService.Register(request.Email, request.Password);
            return Ok(new { user.Id, user.Email });
        }

        /// <summary>
        /// Attempts to log in a user with the given credentials.
        /// </summary>
        /// <param name="request">The login request containing email and password.</param>
        /// <returns>
        /// An HTTP 200 response with user info if credentials are valid; otherwise, HTTP 401.
        /// </returns>
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var user = _userService.Login(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Invalid credentials");

            return Ok(new { user.Id, user.Email });
        }
    }
}
