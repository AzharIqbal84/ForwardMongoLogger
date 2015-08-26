namespace ForwardMongoLogger
{
    using log4net.Layout;
    public class MongoAppenderFileld
    {
        public string Name { get; set; }
        public IRawLayout Layout { get; set; }
    }
}
