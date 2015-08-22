    
using log4net.Layout;

namespace ForwardMongoLogger
{
    public class MongoAppenderFileld
    {
        public string Name { get; set; }
        public IRawLayout Layout { get; set; }
    }
}
