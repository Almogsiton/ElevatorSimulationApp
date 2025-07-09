using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.Services
{
    /// <summary>
    /// Provides user-related operations such as registration and login.
    /// This service handles all logic related to managing users in the system.
    /// </summary>
    public class UserService
    {
        private readonly ElevatorDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing user data.</param>
        public UserService(ElevatorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Attempts to log in a user using the provided email and password.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>
        /// The matching <see cref="User"/> object if credentials are valid;
        /// otherwise, <c>null</c>.
        /// </returns>
        public User? Login(string email, string password)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
        }

        /// <summary>
        /// Registers a new user with the given email and password.
        /// Returns null if the user already exists.
        /// </summary>
        /// <param name="email">The email address to register.</param>
        /// <param name="password">The password for the new user.</param>
        /// <returns>The newly created <see cref="User"/> object, or null if email already exists.</returns>
        public User? Register(string email, string password)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                return null;
            }

            var user = new User
            {
                Email = email,
                Password = password
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

    }
}
