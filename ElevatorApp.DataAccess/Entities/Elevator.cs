using System.Collections.Generic;
using ElevatorApp.DataAccess.Helpers;


namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents an elevator located in a specific building.
    /// </summary>
    public class Elevator
    {
        private int _id;
        private int _buildingId;
        private int _currentFloor = Constants.DefaultGroundFloor;
        private ElevatorStatus _status = ElevatorStatus.Idle;
        private ElevatorDirection _direction = ElevatorDirection.None;
        private DoorStatus _doorStatus = DoorStatus.Closed;
        private Building? _building;
        private List<ElevatorCallAssignment> _assignments = new List<ElevatorCallAssignment>();

        /// <summary>
        /// Gets or sets the unique identifier for the elevator.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of the building where this elevator is located.
        /// </summary>
        public int BuildingId
        {
            get { return _buildingId; }
            set { _buildingId = value; }
        }

        /// <summary>
        /// Gets or sets the current floor the elevator is on.
        /// </summary>
        public int CurrentFloor
        {
            get { return _currentFloor; }
            set { _currentFloor = value; }
        }

        /// <summary>
        /// Gets or sets the current operational status of the elevator.
        /// </summary>
        public ElevatorStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <summary>
        /// Gets or sets the direction the elevator is currently moving.
        /// </summary>
        public ElevatorDirection Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        /// <summary>
        /// Gets or sets the status of the elevator doors (open or closed).
        /// </summary>
        public DoorStatus DoorStatus
        {
            get { return _doorStatus; }
            set { _doorStatus = value; }
        }

        /// <summary>
        /// Gets or sets the building that contains this elevator.
        /// </summary>
        public Building? Building
        {
            get { return _building; }
            set { _building = value; }
        }

        /// <summary>
        /// Gets or sets the list of call assignments associated with this elevator.
        /// </summary>
        public List<ElevatorCallAssignment> Assignments
        {
            get { return _assignments; }
            set { _assignments = value; }
        }
    }
}
