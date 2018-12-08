/*
 * FILE          : AppointmentScheduler.cs
 * PROJECT       : SQ EMS
 * TEAM          : Odysseus
 */

using System;
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
            ValidateDate(time.month, time.week, time.day, time.slot);

            //if the time slot does not have a patient booked add them
            //else throw an exception
            if (GetPatientIDs(time) == null)
            {
                Appointments.Insert(maxID, time.month, time.week, time.day, time.slot, PatientID, CaregiverID);
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
        /// <param name="nweeks">The target number of weeks after the start</param>
        /// <returns>The timeslot, or null if none was found.</returns>
        public AptTimeSlot FindNextSlot(AptTimeSlot after, int nweeks)
        {
            int month = after.month;
            int week = after.week + 1;
            int day = 0;
            int slot = 0;

            //validate the given time slot
            ValidateDate(after.month, after.week, after.day, after.slot);

            //Loop until the target week
            while (nweeks > 0)
            {
                //loop through the days
                while (day < 7)
                {
                    //loop through the time slots
                    while (slot < CalendarInfo.MAX_APPOINTMENTS[day])
                    {
                        
                        //if the time slot is full then increment the slot
                        //else return the time slot found
                        AptTimeSlot tmpTime = new AptTimeSlot(month, week, day, slot);
                        if (GetPatientIDs(tmpTime) != null)
                        {
                            slot++;
                        }
                        else
                        {
                            return tmpTime;
                        }
                    }
                    //reset the slot number for the next day and increment day
                    slot = 0;
                    day++;
                }

                //reset the day and increment the week
                //decrement the target week
                day = 0;
                nweeks--;
                week++;

                //if the week exceeds 4 (4 weeks in a month) then reset week and increment the month
                if (week >= 4)
                {
                    week = 0;
                    month++;
                }
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
            ValidateDate(slot.month, slot.week, slot.day, slot.slot);

            //Find the appointment being searched
            //if the PatientID and the CaregiverID is valid thn return the IDs
            //else log an error
            foreach (object key in Appointments.WhereEquals("Month;Week;Day;TimeSlot", slot.month, slot.week, slot.day, slot.slot))
            {
                int.TryParse(Appointments[key, "PatientID"].ToString(), out int pID);
                int.TryParse(Appointments[key, "CaregiverID"].ToString(), out int cGiverID);

                retIDs = new Tuple<int, int>(pID, cGiverID);

            }

            return retIDs;
        }





        /// <summary>
        /// Gets the number of appointments for a given day.
        /// </summary>
        /// <param name="week">The week to check</param>
        /// <param name="day">The day to check</param>
        /// <returns>The number of appointments</returns>
        public int AppointmentCount(int month, int week, int day)
        {
            int retInt = 0;
            string[] columns = { "Month", "Week", "Day" };

            //validate the given date
            ValidateDate(month, week, day);

            //Find all apointments in the given date and increment the int for each of them
            foreach (object key in Appointments.WhereEquals("Month;Week;Day", month, week, day))
            {
                retInt++;
            }

            return retInt;
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
        public bool ValidateDate (int month = 0, int week = 0, int day = 0, int slot = 0)
        {
            bool valid = false;

            //check that the dates are within range and return true
            if ( -1 < month && -1 < week && week < 4 && -1 < day && day < 7 && -1 < slot && slot < CalendarInfo.MAX_APPOINTMENTS[day])
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
    }
}

