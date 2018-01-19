using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace Sloader.Switches
{
    class AvayaCommands 
    {
       public enum Commands
        {
            Cancel,
            Refresh,
            Enter,
            ClearField,
            Help,
            NextPage,
            PreviousPage,
            ListStation,
            DisplayStation,
            ListConfigStation
        }

        public string Command(Commands cmd)
        {
            switch(cmd)
            {
                //0x18 = ESC
                case Commands.Cancel:
                    return string.Format("{0}[3~", (char)0x1B);
                case Commands.Refresh:
                    return string.Format("{0}[34~", (char)0x1B);
                case Commands.Enter:
                    return string.Format("{0}[29~", (char)0x1B);
                case Commands.ClearField:
                    return string.Format("{0}[33~", (char)0x1B);
                case Commands.Help:
                    return string.Format("{0}[28~", (char)0x1B);
                case Commands.NextPage:
                    return string.Format("{0}[6~", (char)0x1B);
                case Commands.PreviousPage:
                    return string.Format("{0}[5~", (char)0x1B);
                case Commands.ListStation:
                    return "list stat";
                case Commands.DisplayStation:
                    return "disp stat ";
                case Commands.ListConfigStation:
                    return "list config stat";
                default:
                    return "";
            }
        }
    }
}
