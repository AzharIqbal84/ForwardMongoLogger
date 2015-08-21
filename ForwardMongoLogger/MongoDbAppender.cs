using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Appender;
using log4net.Core;

namespace ForwardMongoLogger
{
    public class MongoDbAppender : AppenderSkeleton
    {
        private readonly List<MongoAppenderFileld> _fields = new List<MongoAppenderFileld>();
        
        #region Public Members
        /// <summary>
        /// MongoDB database connection in the format:
        /// mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
        /// See http://www.mongodb.org/display/DOCS/Connections
        /// If no database specified, default to "log4net"
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The connectionString name to use in the connectionStrings section of your *.config file
        /// If not specified or connectionString name does not exist will use ConnectionString value
        /// </summary>
        public string ConnectionStringName { get; set; }

        /// <summary>
        /// Name of the collection in database
        /// Defaults to "logs"
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Specify if use cappedCollection
        /// </summary>
        public string CappedCollection { get; set; }

        /// <summary>
        /// Size of CappedCollection in Bytes
        /// </summary>
        public string CappedCollectionSize { get; set; }

        public void AddField(MongoAppenderFileld fileld)
        {
            _fields.Add(fileld);
        }

        #endregion
        
        #region Protected Members
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            var mongoDbHelper = new MongoDbHelper();

            // get connectionString from config file
            var connectionString = mongoDbHelper.GetConnectionString(ConnectionStringName, ConnectionString);

            // get database
            var db = mongoDbHelper.GetDatabase(connectionString);


            var collection = mongoDbHelper.GetCollection(db, CappedCollection != null && Convert.ToBoolean(CappedCollection),CollectionName,CappedCollectionSize != null? long.Parse(CappedCollectionSize):0);
 

             //build Bson document
            var bsonDocument = mongoDbHelper.BuildBsonDocument(loggingEvent);
            
            // insert doc in db
            mongoDbHelper.InsertDocumentInCollection(bsonDocument, collection);
        }
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            var mongoDbHelper = new MongoDbHelper();

            // get connectionString from config file
            var connectionString = mongoDbHelper.GetConnectionString(ConnectionStringName, ConnectionString);
            
            // get database
            var db = mongoDbHelper.GetDatabase(connectionString);

            // get collection
            var collection = mongoDbHelper.GetCollection(db, Convert.ToBoolean(CappedCollection), CollectionName, long.Parse(CappedCollectionSize));

            //// build Bson documents
            //var bsonDocuments = loggingEvents.Select(mongoDbHelper.BuildBsonDocument).ToList();
            
            //// insert docs in db
            //mongoDbHelper.InsertDocumentsInCollection(bsonDocuments, collection);
        }
        
        #endregion
      
      }
}
