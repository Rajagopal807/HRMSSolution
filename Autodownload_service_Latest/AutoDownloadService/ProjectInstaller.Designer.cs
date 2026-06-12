namespace AutodownloadService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AutodownloadService = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // AutodownloadService
            // 
            this.AutodownloadService.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.AutodownloadService.Password = null;
            this.AutodownloadService.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Service DOwnloaded the Punches and Push to LMS and SAP.";
            this.serviceInstaller1.DisplayName = "Autodownload Serivce";
            this.serviceInstaller1.ServiceName = "AutodownloadService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.AutodownloadService,
            this.serviceInstaller1});
        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller AutodownloadService;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}