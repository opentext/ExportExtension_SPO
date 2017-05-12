using System;
using ExportExtensionCommon;
using System.Drawing;

namespace CaptureCenter.SPO
{
    public class SPOFactory : SIEEFactory
    {
        public override SIEESettings CreateSettings() { return new SPOSettings(); }
        public override SIEEUserControl CreateWpfControl() { return new SPOControlWPF(); }
        public override SIEEViewModel CreateViewModel(SIEESettings settings)
        { 
            return new SPOViewModel(settings, new SPOClient());
        }
        public override SIEEExport CreateExport() { return new SPOExport(new SPOClient()); }
        public override SIEEDescription CreateDescription() { return new SPODescription(); }
    }

    class SPODescription : SIEEDescription
    {
        public override string TypeName { get { return "SharePoint (online)"; } }

        public override string DefaultNewName { get { return "SPO"; } }

        public override string GetLocation(SIEESettings s)
        {
            return ((SPOSettings)s).SiteUrl;
        }

        public override void OpenLocation(string location)
        {
            System.Diagnostics.Process.Start(location);
        }

        public override Image Image
        {
            get { return Properties.Resources.SPOLogo; }
        }
    }
}
