using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileScouter
{
    // utility class for performing action when x button on console window used
    // could be extened to perform actions based on other events passed to the SetConsoleCtrlHandler
    internal class ConsoleCloseHandler
    {
        // make delegate to handle function needed later, this needs to store the external function from Windows, so return type and param need to match for that function being stored
        private delegate bool ConsoleEventHandler(int eventType);
        // make variable from delegate created to contain function later
        private static ConsoleEventHandler handler;
        // close event we're interested in, the x on console window
        private const int CTRL_CLOSE_EVENT = 2;

        // native Window OS library needed to access console event method
        [DllImport("kernel32.dll")]
        // extern needed to let program know this is a DLL method for the attribute above
        private static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);

        // runs registered function whenever console x is clicked
        public static void RegisterCallback(Action onCloseWithX)
        {
            // function passed is assigned to delgate, without if statement, this would just be assigning the method passed
            // this makes the passed in function to RegisterCallback the function used for handling the Windows function for console events
            handler = eventType =>
            {
                // only run for close with x, not good to run code for any of the events sent
                if (eventType == CTRL_CLOSE_EVENT)
                {
                    // runs delegate method
                    onCloseWithX?.Invoke();
                }

                // allows process to shutdown
                return false;
            };

            // handler used everytime console x is clicked
            SetConsoleCtrlHandler(handler, true);
        }
     
    }
}
