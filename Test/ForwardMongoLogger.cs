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
            ILog log = LogManager.GetLogger(typeof(ForwardMongoLogger));

            log.Info("some info ");
        }
    }
}
