using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingUI
{
    /// <summary>
    /// Handles the entry of billing codes and appointment recall.
    /// </summary>
    public class BillingController : TernaryContainer, IInterfaceContent
    {
        public string Name => "BillingCodeSelector";
        
        public event EventHandler<ReferenceArgs<IEnumerable<object>>> Finish;

        private InputController controller = new InputController();

        private LinkedList<string> codes = new LinkedList<string>();
        
        private Label Description = new Label("Add or Remove Billing Codes")
        {
            Center = true,
            PreferredHeight = 3
        };

        private GridContainer bottom_container = new GridContainer()
        {
            CountX = 2
        };

        private Button AddCodeButton = new Button()
        {
            Center = true,
            Text = "Add Code"
        };

        private Button FinishButton = new Button()
        {
            Center = true,
            Text = "Finish"
        };

        public BillingController(params string[] codes)
        {
            bottom_container.Add(AddCodeButton, FinishButton);

            Add(Description, bottom_container);

            First = Description;
            Third = bottom_container;

            Array.ForEach(codes, s => this.codes.AddLast(s));

            //TODO finish this
        }

        public void Activate()
        {
            controller.Activate();
        }

        public void Deactivate()
        {
            controller.Deactivate();
        }

        public void Initialize(RootContainer root)
        {

        }
        
    }
    
}
