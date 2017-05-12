using System;
using System.Net.NetworkInformation;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    public class SPOConnectionTestHandler : ConnectionTestHandler
    {
        public SPOConnectionTestHandler(VmTestResultDialog vmTestResultDialog) : base(vmTestResultDialog)
        {
            TestList.Add(new TestFunctionDefinition()
                { Name = "Try to reach Process Suite (ping)", Function = TestFunction_Ping, ContinueOnError = true });
            TestList.Add(new TestFunctionDefinition()
                { Name = "Try to log in", Function = TestFunction_Login });
            TestList.Add(new TestFunctionDefinition()
                { Name = "Try to read some information", Function = TestFunction_Read });
        }

        #region The test fucntions
        ISPOClient SPOClient
        {
            get{ return ((SPOViewModel_CT)CallingViewModel).GetSPOClient(); }
        }

        private bool TestFunction_Ping(ref string errorMsg)
        {
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(SPOClient.GetServer());
                if (reply.Status != IPStatus.Success)
                {
                    errorMsg = "Return status = " + reply.Status.ToString();
                    return false;
                }
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
                if (e.InnerException != null) errorMsg += "\n" + e.InnerException.Message;
                return false;
            }
            return true;
        }

        private bool TestFunction_Login(ref string errorMsg)
        {
            try
            {
                SPOClient.Login();
            }
            catch (Exception e)
            {
                errorMsg = "Could not log in. \n" + e.Message;
                if (e.InnerException != null)
                    errorMsg += "\n" + e.InnerException.Message;
                return false;
            }
            return true;
        }

        private bool TestFunction_Read(ref string errorMsg)
        {
            try
            {
                SPOClient.GetLists();
            }
            catch (Exception e)
            {
                errorMsg = "Could not read lists\n" + e.Message;
                if (e.InnerException != null)
                    errorMsg += "\n" + e.InnerException.Message;
                return false;
            }
            return true;
        }
        #endregion
    }
}
