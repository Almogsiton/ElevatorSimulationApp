using System;
using ElevatorApp.DataAccess.Helpers;


namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents the assignment of an elevator to a specific elevator call.
    /// </summary>
    public class ElevatorCallAssignment
    {
        private int _id;
        private int _elevatorId;
        private int _elevatorCallId;
        private DateTime _assignmentTime;
        private Elevator? _elevator;
        private ElevatorCall? _elevatorCall;

        /// <summary>
        /// Gets or sets the unique identifier for this assignment.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of the elevator assigned to the call.
        /// </summary>
        public int ElevatorId
        {
            get { return _elevatorId; }
            set { _elevatorId = value; }
        }

        /// <summary>
        /// Gets or sets the ID of the elevator call that this assignment is linked to.
        /// </summary>
        public int ElevatorCallId
        {
            get { return _elevatorCallId; }
            set { _elevatorCallId = value; }
        }

        /// <summary>
        /// Gets or sets the time the assignment was created.
        /// </summary>
        public DateTime AssignmentTime
        {
            get { return _assignmentTime; }
            set { _assignmentTime = value; }
        }

        /// <summary>
        /// Gets or sets the elevator assigned to the call.
        /// </summary>
        public Elevator? Elevator
        {
            get { return _elevator; }
            set { _elevator = value; }
        }

        /// <summary>
        /// Gets or sets the elevator call that is being handled.
        /// </summary>
        public ElevatorCall? ElevatorCall
        {
            get { return _elevatorCall; }
            set { _elevatorCall = value; }
        }
    }
}
