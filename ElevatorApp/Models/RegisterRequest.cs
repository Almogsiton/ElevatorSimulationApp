namespace ElevatorApp.Models
{
    /// <summary>
    /// Represents the data required to register a new user in the system.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the email address to be used for the new account.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password to be used for the new account.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
