/*
* FILE          : DatabaseWrapper.cs
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Austin Zalac
* FIRST VERSION : November 15, 2018
*/
using Billing;
using Definitions;
using Demographics;
using Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    /// <summary>
    /// A wrapper which contains an instance of all database interaction classes,
    /// with methods to help bridge between them and the interface.
    /// </summary>
    public class DatabaseWrapper
    {
        //Class level variables
        private AppointmentScheduler scheduler;
        private Billing.Billing billing;
        private BillingFileInteraction billingIO;
        private PersonDB people;
        private HouseholdManager houses;

        /// <summary>
        /// A constructor that initializes class level variables
        /// </summary>
        /// 
        public DatabaseWrapper(DatabaseManager database)
        {
            scheduler = new AppointmentScheduler(database);
            billing = new Billing.Billing(database);
            billingIO = new BillingFileInteraction(database);
            people = new PersonDB(database);
            houses = new HouseholdManager(database);
        }

        /// <summary>
        /// Gets the appointment count
        /// </summary>
        /// <param name="month">The month of appointment</param>
        /// <param name="day">The day of appointment</param>
        /// <returns>appointment count</returns>
        public int? GetAppointmentCount(int month, int day)
        {
            return scheduler.AppointmentCount(month, day);
        }

        /// <summary>
        /// Gets the appointment count on a specific day
        /// </summary>
        /// <param name="month">The month of appointment</param>
        /// <param name="day">The day of appointment</param>
        /// <returns>patients id (All day) </returns>
        public Tuple<int, int, int>[] GetAppointmentsOnDay(int month, int day)
        {
            return scheduler.GetPatientIDs_AllDay(new AptTimeSlot(month, day, 0));
        }
        
        /// <summary>
        /// Finds people based on the given arguments.
        /// </summary>
        /// <param name="firstname">The firstname.</param>
        /// <param name="initial">The middle initial.</param>
        /// <param name="lastname">The lastname.</param>
        /// <param name="phonenumber">The phonenumber.</param>
        /// <param name="hcn">The health card number/</param>
        /// <returns>All people who match</returns>
        public object FindPerson(string firstname, char? initial, string lastname, string phonenumber, string hcn)
        {
            return people.Find(firstname, initial, lastname, phonenumber, hcn).FirstOrDefault();
        }

        /// <summary>
        /// Adds a person
        /// </summary>
        /// <param name="firstname">The firstname.</param>
        /// <param name="initial">The middle initial.</param>
        /// <param name="lastname">The lastname.</param>
        /// <param name="dob">Date of birth.</param>
        /// <param name="sex">Gender of person.</param>
        /// <param name="hcn">The health card number/</param>
        /// <returns>Patient added</returns>
        public object AddPerson(string firstname, char initial, string lastname, string dob, char sex, string hcn)
        {
            return people.CreatePatient(hcn, lastname, firstname, initial, dob,
                (SexTypes)Enum.Parse(typeof(SexTypes), initial.ToString()), 0);
        }
        
        /// <summary>
        /// Gets the billing codes for an appointment.
        /// </summary>
        /// <param name="appointmentid">The appointment.</param>
        /// <returns>The codes.</returns>
        public string[] GetBillingCodesForApt(int appointmentid)
        {
            return billing.GetBillableProceduresFor(appointmentid).ToArray();
        }

        /// <summary>
        /// Sets the codes for a given appointment by determining the deltas
        /// and removing them or adding them, depending on how they are delta-d.
        /// 
        /// Does nothing if the codes haven't changed.
        /// </summary>
        /// <param name="appointmentid">The appointment to update.</param>
        /// <param name="codes">The codes to set.</param>
        public void SetBillingCodesForApt(int appointmentid, string[] codes)
        {
            HashSet<string> current = new HashSet<string>(GetBillingCodesForApt(appointmentid));

            HashSet<string> requested = new HashSet<string>(codes);

            // ignore any codes that haven't changed
            HashSet<string> intersection = new HashSet<string>(current.Intersect(requested));
            foreach(string intersect in intersection)
            {
                current.Remove(intersect);
                requested.Remove(intersect);
            }

            // codes which are in current, but not requested should be removed
            foreach(string remove in current)
            {
                billing.RemoveBillingCode(appointmentid, remove);
            }

            // codes which are in requested, but not current should be added
            foreach(string add in current)
            {
                billing.AddBillingCode(appointmentid, add);
            }
        }

        /// <summary>
        /// Method that generates billing file
        /// </summary>
        /// <param name="month">Month of billing file</param>
        /// <param name="path">Path being written to</param>
        /// 

        public void GenerateBillingFile(int month, string path)
        {
            billingIO.GenerateMonthlyBillingFile(month, path);
        }

        /// <summary>
        /// Method that does the billing reconcile
        /// </summary>
        /// <param name="month">Month of billing file</param>
        /// <param name="responsepath">Path being written to</param>
        /// <returns>The parsed response</returns>
        /// 

        public bool DoBillingReconcile(int month, string responsepath)
        {
            return billingIO.ParseResponse(month, responsepath);
        }

        /// <summary>
        /// Method that finds the household
        /// </summary>
        /// <param name="address1">Address line 1</param>
        /// <param name="address2">Address line 2</param>
        /// <param name="city">City of household</param>
        /// <param name="province">Province of household</param>
        /// <param name="city">City of household</param>
        /// <returns>Object containing information of household</returns>
        /// 

        public object FindHousehold(string address1, string address2, string city,
            string province, string phonenum)
        {
            return houses.FindHousehold(address1, address2, city,
                province, phonenum);
        }

        /// <summary>
        /// Method that adds the household
        /// </summary>
        /// <param name="address1">Address line 1</param>
        /// <param name="address2">Address line 2</param>
        /// <param name="city">City of household</param>
        /// <param name="province">Province of household</param>
        /// <param name="phonenum">Phone number of household</param>
        /// <param name="HOH_HCN">HCN of head of house</param>
        /// <returns>Object containing information of adding household</returns>
        /// 

        public object AddHousehold(string address1, string address2, string city,
            string province, string phonenum, string HOH_HCN)
        {
            return houses.AddHousehold(address1, address2, city,
                province, phonenum, HOH_HCN);
        }

        /// <summary>
        /// Method that compiles the billing summary of a specified month
        /// </summary>
        /// <param name="month">Month being generated</param>
        /// <returns>string of information</returns>
        ///

        public string CompileSummary(int month)
        {
            return billingIO.CompileStatistics(month);
        }

        /// <summary>
        /// Method that returns available time slots
        /// </summary>
        /// <param name="year">Year being requested</param>
        /// <param name="month">Month being requested</param>
        /// <param name="day">Day being requested</param>
        /// <param name="slot">Slot being requested</param>
        /// <returns>bool if timeslot is availale</returns>
        ///
        public bool TimeslotAvailable(int year, int month, int day, int slot)
        {
            return scheduler.TimeslotAvailable(CalendarManager.ConvertYearMonthToMonth(year, month), day, slot);
        }

        /// <summary>
        /// Method that gets the appointment ID
        /// </summary>
        /// <param name="year">Year being requested</param>
        /// <param name="month">Month being requested</param>
        /// <param name="day">Day being requested</param>
        /// <param name="slot">Slot being requested</param>
        /// <returns>appointment ID</returns>
        ///

        public int? GetAppointmentID(int year, int month, int day, int slot)
        {
            return scheduler.GetAppointmentID(CalendarManager.ConvertYearMonthToMonth(year, month), day, slot);
        }

        /// <summary>
        /// Method schedules the appointment
        /// </summary>
        /// <param name="year">Year being requested</param>
        /// <param name="month">Month being requested</param>
        /// <param name="day">Day being requested</param>
        /// <param name="slot">Slot being requested</param>
        /// <param name="patientid">ID of patient</param>
        /// <param name="caregiverid"> ID of caregiver</param>
        ///
        public void ScheduleAppointment(int year, int month, int day, int slot, int patientid, int caregiverid)
        {
            scheduler.Schedule(new AptTimeSlot(CalendarManager.ConvertYearMonthToMonth(year, month), day, slot), patientid, caregiverid);
        }

        /// <summary>
        /// Method that reschedules appointments
        /// </summary>
        /// <param name="aptid">ID of appointment</param>
        /// <param name="year">Year being requested</param>
        /// <param name="month">Month being requested</param>
        /// <param name="day">Day being requested</param>
        /// <param name="slot">Slot being requested</param>
        ///

        public void RescheduleAppointment(int aptid, int year, int month, int day, int slot)
        {
            scheduler.Reschedule(aptid, new AptTimeSlot(CalendarManager.ConvertYearMonthToMonth(year, month), day, 0));
        }

        /// <summary>
        /// Method that finds next empty slot
        /// </summary>
        /// <param name="year">Year being requested</param>
        /// <param name="month">Month being requested</param>
        /// <param name="day">Day being requested</param>
        /// <param name="nweeks_after">Number of weeks after current appointment date</param>
        ///
        public AptTimeSlot FindNextEmpty(int year, int month, int day, int nweeks_after)
        {
            return scheduler.FindNextSlot(new AptTimeSlot(CalendarManager.ConvertYearMonthToMonth(year, month), day, 0), nweeks_after);
        }

        /// <summary>
        /// Method that sets the household
        /// </summary>
        /// <param name="person">Person being set</param>
        /// <param name="house">House that person resides</param>
        ///
        public void SetHousehold(int person, int house)
        {
            houses.SetHousehold(person, house);
        }
    }
}
