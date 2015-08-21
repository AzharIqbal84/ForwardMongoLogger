using System;
using log4net;
using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ForwardMongoLogger
    {
        [TestMethod]
        public void Log_Info_Message()
        {
            XmlConfigurator.Configure();
            log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            log.Info("some infor");
            log.Error("some error", new Exception("this is explicit launched exception"));
          
        }
    }
}
