using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Recevied_Mail
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            // Service will run under the system account
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Service Information
            serviceInstaller.DisplayName = "Recevied Mail Service";
            serviceInstaller.ServiceName = "ReceviedMailService";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add installers to collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
