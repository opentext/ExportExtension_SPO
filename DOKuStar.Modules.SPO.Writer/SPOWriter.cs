using System;
using RightDocs.Common;
using ExportExtensionCommon;
using CaptureCenter.SPO;

namespace DOKuStar.SPO
{
    [CustomExportDestinationDescription("SPOWriter", "ExportExtensionInterface", "SIEE based Writer for SharePoint (online) Export", "OpenText")]
    public class SPOWriter: EECExportDestination
    {
        public SPOWriter() : base()
        {
            Initialize(new SPOFactory());
        }
    }
}