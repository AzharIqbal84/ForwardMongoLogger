using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ForwardMongoLoggerTest
    {
        log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [TestInitialize]
        public void SetUp()
        {
            XmlConfigurator.Configure();
        }


        [TestMethod]
        public void Log_Info_Test()
        {
            _log.Info("some infor");
        }
    }
}
