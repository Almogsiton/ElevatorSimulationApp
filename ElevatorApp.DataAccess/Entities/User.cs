using System.Collections.Generic;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents an authenticated user in the system, who owns one or more buildings.
    /// </summary>
    public class User
    {
        private int _id;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private List<Building> _buildings = new List<Building>();

        /// <summary>
        /// Gets or sets the unique system-generated identifier for the user.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the email address used for authentication and communication.
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        /// <summary>
        /// Gets or sets the hashed password used to verify the user's identity.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// Gets or sets the collection of buildings managed by this user.
        /// </summary>
        public List<Building> Buildings
        {
            get { return _buildings; }
            set { _buildings = value; }
        }
    }
}
