using System;

namespace Support
{
	/// <summary>
	/// An appointment's time slot
	/// </summary>
	public class AptTimeSlot
	{
		public int month,
                   week,
				   day,
				   slot;

        public AptTimeSlot(int month, int week, int day, int slot)
        {
            this.month = month;
            this.week = week;
            this.day = day;
            this.slot = slot;
        }
	}

    public class AppointmentScheduler
    {
        private DatabaseTable Patients;
        private DatabaseTable Appointments;

        public AppointmentScheduler(DatabaseManager database)
        {
            Patients = database["Patients"];
            Appointments = database["Appointments"];
        }

        /// <summary>
        /// Schedules an appointment at the specified time.
        /// </summary>
        /// <param name="time">The time</param>
        /// <param name="PatientID">The patient</param>
        /// <param name="CaregiverID">The patient's caregiver</param>
        /// <exception cref="ArgumentException">When the time is already filled.</exception>
        public void Schedule(AptTimeSlot time, int PatientID, int CaregiverID)
        {

        }

        /// <summary>
        /// Finds the next available time slot.
        /// </summary>
        /// <param name="after">The time to start from</param>
        /// <param name="nweeks">The target number of weeks after the start</param>
        /// <returns>The timeslot, or null if none was found.</returns>
        public AptTimeSlot FindNextSlot(AptTimeSlot after, int nweeks)
        {
            return null;
        }

        /// <summary>
        /// Gets the patients for a given appointment.
        /// </summary>
        /// <param name="slot">The time slot to check</param>
        /// <returns>The patient IDs for the patients, or null if the appointment isn't scheduled.</returns>
        public Tuple<int, int> GetPatientIDs(AptTimeSlot slot)
        {
            
            return null;
        }

        /// <summary>
        /// Gets the number of appointments for a given day.
        /// </summary>
        /// <param name="week">The week to check</param>
        /// <param name="day">The day to check</param>
        /// <returns>The number of appointments</returns>
        public int AppointmentCount(int month, int week, int day)
        {
            int[] toSearch = { month, week, day};
            string[] columns = { "Month", "Week", "Day"};

            int retInt = 0;

            foreach (object key in Appointments.WhereEquals(columns, toSearch))
            {
                retInt++;
            }

            return retInt;
        }

    }
}

