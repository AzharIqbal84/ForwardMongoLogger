using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Config;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            log.Info("some infor");
            try
            {
                var op1= 10;
                var op2 = 0;
                var res = op1 / op2;
            }
            catch (Exception ex)
            {

                log.Error("some infor",ex);
            }
            Console.Read();
        }
    }
}
