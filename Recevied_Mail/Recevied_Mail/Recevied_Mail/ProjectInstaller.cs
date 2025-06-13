using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Recevied_Mail
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set the account type under which the service should run
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            // Set service properties
            serviceInstaller.ServiceName = "ReceivedMailService";
            serviceInstaller.DisplayName = "Received Mail Service";
            serviceInstaller.Description = "This service fetches emails from Gmail and stores them in SQL Server.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }

}
