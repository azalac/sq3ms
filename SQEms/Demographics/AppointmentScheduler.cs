/*
* FILE          : Person.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Mike Ramoutsakis
* FIRST VERSION : November 20, 2018
*/

using System;
using System.Linq;
using Definitions;

namespace Support
{
    /// <summary>
    /// NAME: AptTimeSlot
    /// PURPOSE: Holds an appointments date and slot
    /// </summary>
    public class AptTimeSlot
	{
		public int month,
				   day,
				   slot;

        public AptTimeSlot(int month, int day, int slot)
        {
            this.month = month;
            this.day = day;
            this.slot = slot;
        }
	}




    /// <summary>
    /// NAME: AppointmentScheduler
    /// PURPOSE: This class will handle setting appointments for a patient,
    ///             find the next available time slot, get a patients ID,
    ///             and give the number of appointments in a given day
    /// </summary>
    public class AppointmentScheduler
    {
        private readonly DatabaseTable Patients;
        private DatabaseTable Appointments;


       /** <summary>
         * AppointmentScheduler constructor
         * </summary>
         * <param name="database">The database manager being used within the class</param>
         */
        public AppointmentScheduler(DatabaseManager database)
        {
            Patients = database["Patients"];
            Appointments = database["Appointments"];
        }





        /// <summary>
        /// Schedules an appointment at the specified time.
        /// </summary>
        /// <param name="time">The appointments time</param>
        /// <param name="PatientID">The patient being booked</param>
        /// <param name="CaregiverID">The patient's caregiver</param>
        /// <exception cref="ArgumentException">When the time is already filled.</exception>
        public void Schedule(AptTimeSlot time, int PatientID, int CaregiverID)
        {
            //Get the next Patients id
            int maxID = Appointments.GetMaximum("AppointmentID") + 1;

            //validate the date
            ValidateDate(time.month, time.day, time.slot);

            //if the time slot does not have a patient booked add them
            //else throw an exception
            if (GetPatientIDs(time) == null)
            {
                Appointments.Insert(maxID, time.month, time.day, time.slot, PatientID, CaregiverID);
            }
            else
            {
                throw new System.ArgumentException("Time slot already filled");
            }
        }





        /// <summary>
        /// Finds the next available time slot.
        /// </summary>
        /// <param name="after">The time to start from</param>
        /// <param name="weeks_to_skip">The target number of weeks to skip</param>
        /// <returns>The timeslot, or null if none was found.</returns>
        public AptTimeSlot FindNextSlot(AptTimeSlot after, int weeks_to_skip, int max_months = 1)
        {
            int month = after.month;
            // start the sunday of 'after'
            int day = after.day - after.day % CalendarInfo.WEEK_LENGTH +
                weeks_to_skip * CalendarInfo.WEEK_LENGTH;

            //validate the given time slot
            ValidateDate(after.month, after.day, after.slot);

            //Loop until the maximum months exceeded
            while (month - after.month < max_months)
            {
                //loop through every day in the month
                while (day < DateTime.DaysInMonth(month / 12, month % 12))
                {
                    //loop through the time slots for the current day
                    for (int slot = 0; slot < CalendarInfo.MAX_APPOINTMENTS[day % CalendarInfo.WEEK_LENGTH]; slot++)
                    {
                        //if the time slot isn't full then return the time slot found
                        AptTimeSlot tmpTime = new AptTimeSlot(month, day, slot);
                        if (GetPatientIDs(tmpTime) == null)
                        {
                            return tmpTime;
                        }
                    }

                    day++;
                }

                //reset the day and increment the week
                //decrement the target week
                day = 0;
                month++;
            }

            return null;
        }





        /// <summary>
        /// Gets the patients of a given appointment.
        /// </summary>
        /// <param name="slot">The time slot to check</param>
        /// <returns>The patients and the caregivers IDs, or null if the appointment isn't scheduled.</returns>
        public Tuple<int, int> GetPatientIDs(AptTimeSlot slot)
        {
            //the value to be returned
            Tuple<int, int> retIDs = null;

            //validate the time slot
            ValidateDate(slot.month, slot.day, slot.slot);

            //Find the appointment being searched
            //if the PatientID and the CaregiverID is valid thn return the IDs
            //else log an error
            foreach (object key in Appointments.WhereEquals("Month;Day;TimeSlot", slot.month, slot.day, slot.slot))
            {
                int.TryParse(Appointments[key, "PatientID"].ToString(), out int pID);
                int.TryParse(Appointments[key, "CaregiverID"].ToString(), out int cGiverID);

                retIDs = new Tuple<int, int>(pID, cGiverID);

            }

            return retIDs;
        }

        /// <summary>
        /// Reschedules an appointment.
        /// </summary>
        /// <param name="aptid">The appointment.</param>
        /// <param name="new_time">The new time.</param>
        public void Reschedule(int aptid, AptTimeSlot new_time)
        {
            int pid = (int)Appointments[aptid, "PatientID"];
            int cid = (int)Appointments[aptid, "CaregiverID"];

            Schedule(new_time, pid, cid);
        }




        /// <summary>
        /// Gets all appointments on a given day.
        /// </summary>
        /// <param name="slot">The timeslot (slot field ignored).</param>
        /// <returns>A tuple array containing [PatientID, CaregiverID, TimeSlot] for each appointment.</returns>
        public Tuple<int, int, int>[] GetPatientIDs_AllDay(AptTimeSlot slot)
        {
            return Appointments.WhereEquals("Month;Day", slot.month, slot.day)
                .Select(pk => new Tuple<int, int, int>((int)Appointments[pk, "PatientID"], (int)Appointments[pk, "CaregiverID"],
                (int)Appointments[pk, "TimeSlot"])).ToArray();
        }
        

        /// <summary>
        /// Gets the number of appointments for a given day.
        /// </summary>
        /// <param name="week">The week to check</param>
        /// <param name="day">The day to check</param>
        /// <returns>The number of appointments</returns>
        public int AppointmentCount(int month, int day)
        {
            return Appointments.WhereEquals("Month;Day", month, day).Count();
        }





        /// <summary>
        /// Validates the given dates
        /// </summary>
        /// <param name="month">The month to check</param>
        /// <param name="week">The week to check</param>
        /// <param name="day">The day to check</param>
        /// <param name="slot">The slot to check</param>
        /// <exception cref="ArgumentException">If the date is not valid</exception>
        /// <returns>Bool if the dates are valid</returns>
        private bool ValidateDate (int month = 0, int day = 0, int slot = 0)
        {
            bool valid = false;

            //check that the dates are within range and return true
            if ( -1 < month && -1 < day && day < 7 && -1 < slot && slot < CalendarInfo.MAX_APPOINTMENTS[day])
            {
                valid = true;
            }

            //if they are not valid then throw an exception
            else
            {
                throw new System.ArgumentException("Invalid date given");
            }

            return valid;
        }

        /// <summary>
        /// Checks if a timeslot is available.
        /// </summary>
        /// <param name="absmonth">The absolute month (since 1970)</param>
        /// <param name="day">The day of the month</param>
        /// <param name="slot">The timeslot</param>
        /// <returns>Whether the timeslot is available or not</returns>
        public bool TimeslotAvailable(int absmonth, int day, int slot)
        {
            var matches = Appointments.WhereEquals("Month;Day;TimeSlot", absmonth, day, slot);

            return matches.Count() == 0;
        }

        /// <summary>
        /// Gets the appointment during a specific time slot.
        /// </summary>
        /// <param name="absmonth">The absolute month (since 1970)</param>
        /// <param name="day">The day of the month</param>
        /// <param name="slot">The timeslot</param>
        /// <returns>Whether the appointment id</returns>
        public int? GetAppointmentID(int absmonth, int day, int slot)
        {
            var matches = Appointments.WhereEquals("Month;Day;TimeSlot", absmonth, day, slot)
                .Select(obj => new int?((int)obj));

            return matches.FirstOrDefault();
        }
    }
}

