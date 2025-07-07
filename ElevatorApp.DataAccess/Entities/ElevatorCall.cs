using System;
using System.Collections.Generic;
using ElevatorApp.DataAccess.Helpers;


namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents a request for an elevator to pick up or deliver a passenger.
    /// </summary>
    public class ElevatorCall
    {
        private int _id;
        private int _buildingId;
        private int _requestedFloor;
        private int? _destinationFloor;
        private DateTime _callTime;
        private bool _isHandled;
        private Building? _building;
        private List<ElevatorCallAssignment> _assignments = new List<ElevatorCallAssignment>();

        /// <summary>
        /// Gets or sets the unique identifier of the elevator call.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the ID of the building where the call was made.
        /// </summary>
        public int BuildingId
        {
            get { return _buildingId; }
            set { _buildingId = value; }
        }

        /// <summary>
        /// Gets or sets the floor from which the elevator was requested.
        /// </summary>
        public int RequestedFloor
        {
            get { return _requestedFloor; }
            set { _requestedFloor = value; }
        }

        /// <summary>
        /// Gets or sets the floor the user wants to go to. Can be null for pickup-only calls.
        /// </summary>
        public int? DestinationFloor
        {
            get { return _destinationFloor; }
            set { _destinationFloor = value; }
        }

        /// <summary>
        /// Gets or sets the time when the call was made.
        /// </summary>
        public DateTime CallTime
        {
            get { return _callTime; }
            set { _callTime = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this call has been handled.
        /// </summary>
        public bool IsHandled
        {
            get { return _isHandled; }
            set { _isHandled = value; }
        }

        /// <summary>
        /// Gets or sets the building where the call was made.
        /// </summary>
        public Building? Building
        {
            get { return _building; }
            set { _building = value; }
        }

        /// <summary>
        /// Gets or sets the list of assignments linking this call to elevators.
        /// </summary>
        public List<ElevatorCallAssignment> Assignments
        {
            get { return _assignments; }
            set { _assignments = value; }
        }
    }
}
