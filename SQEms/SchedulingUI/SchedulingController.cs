using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    /// <summary>
    /// Handles the interface for scheduling a patient.
    /// </summary>
    /// <remarks>
    /// Workflow:
    /// 
    /// Select a timeslot
    /// 
    /// Find the patient & caregiver
    /// 
    /// </remarks>
    public class SchedulingController
    {
    }

    public class TimeSlotSelectionController : GridContainer
    {
        private readonly InputController controller = new InputController();


    }
}
