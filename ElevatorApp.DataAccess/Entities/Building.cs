using System.Collections.Generic;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents a building owned by a user and containing one or more elevators.
    /// </summary>
    public class Building
    {
        private int _id;
        private int _userId;
        private string _name = string.Empty;
        private int _numberOfFloors;
        private User? _user;
        private List<Elevator> _elevators = new List<Elevator>();
        private List<ElevatorCall> _elevatorCalls = new List<ElevatorCall>();

        /// <summary>
        /// Gets or sets the unique identifier of the building.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of the user who owns this building.
        /// </summary>
        public int UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        /// <summary>
        /// Gets or sets the name of the building.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the number of floors in the building.
        /// </summary>
        public int NumberOfFloors
        {
            get { return _numberOfFloors; }
            set { _numberOfFloors = value; }
        }

        /// <summary>
        /// Gets or sets the user who owns this building.
        /// </summary>
        public User? User
        {
            get { return _user; }
            set { _user = value; }
        }

        /// <summary>
        /// Gets or sets the list of elevators in this building.
        /// </summary>
        public List<Elevator> Elevators
        {
            get { return _elevators; }
            set { _elevators = value; }
        }

        /// <summary>
        /// Gets or sets the list of elevator calls associated with this building.
        /// </summary>
        public List<ElevatorCall> ElevatorCalls
        {
            get { return _elevatorCalls; }
            set { _elevatorCalls = value; }
        }
    }
}
