using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Sloader
{
    class MainStart
    {
    [STAThread]
        
    static void Main(string[] args){
        if (args != null && args.Length > 0) {
            
        } else {
            var app = new App();
            app.Run();
        }
    }
    }
}
