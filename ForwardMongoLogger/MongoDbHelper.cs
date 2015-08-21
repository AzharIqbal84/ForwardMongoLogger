using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using log4net.Appender;
using log4net.Core;



namespace ForwardMongoLogger
{
    public class MongoDbHelper
    {
        #region Constants

        private const string CollectionDefaulName = "logs";
        private const string DataBaseDefaulName = "MongoLog";

        #endregion

        public IMongoCollection<BsonDocument> GetCollection(IMongoDatabase mongoDatabase, bool? cappedCollection,
            string collectionName, long cappedCollectionSize)
        {
            IMongoCollection<BsonDocument> retVal = null;

            if (cappedCollection == null || !cappedCollection.Value)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }
            
            if (!IsCollectionExistsAsync(mongoDatabase, collectionName).Result)
            {
                CreateCollectionAsync(mongoDatabase, collectionName, cappedCollectionSize).Wait();
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }

            if (IsCappedCollection(collectionName, mongoDatabase).Result)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }
        

            if (ConvertCollectionToCapped(collectionName, mongoDatabase, cappedCollectionSize).Result)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
            }
            
            return retVal;
        }

        private static async Task<bool> IsCappedCollection(string collectionName, IMongoDatabase mongoDatabase)
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                {"collstats", collectionName}
            });

            var stats = await mongoDatabase.RunCommandAsync(command);
            return stats["capped"].AsBoolean;
        }

        private static async Task<bool> ConvertCollectionToCapped(string collectionName, IMongoDatabase mongoDatabase,long cappedCollectionSize)
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                {"convertToCapped", collectionName},
                {"size",cappedCollectionSize}
            });

            var stats = await mongoDatabase.RunCommandAsync(command);
            return stats["ok"].AsDouble == 1.0;

        }

        private static async Task CreateCollectionAsync(IMongoDatabase mongoDatabase, string collectionName, long cappedCollectionSize)
        {
            var createCollectionOptions = new CreateCollectionOptions()
            {
                Capped = true,
                MaxSize = cappedCollectionSize
            };
            
            await mongoDatabase.CreateCollectionAsync(collectionName ?? CollectionDefaulName, createCollectionOptions);
        }

        public async Task<bool> IsCollectionExistsAsync(IMongoDatabase mongoDatabase,string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);

            //filter by collection name
            var collections = await mongoDatabase.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            //check for existence
            return (await collections.ToListAsync()).Any();
        }
        
        public string GetConnectionString(string connectionStringName, string connectionString)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];
            return connectionStringSetting != null ? connectionStringSetting.ConnectionString : connectionString;
        }

        public IMongoDatabase GetDatabase(string connectionString)
        {
            var url = MongoUrl.Create(connectionString);

            var client = new MongoClient(url);

            var db = client.GetDatabase(url.DatabaseName ?? DataBaseDefaulName);

            return db;
        }

        public BsonDocument BuildBsonDocument(LoggingEvent log, IEnumerable<MongoAppenderFileld> fields)
        {
            var doc = new BsonDocument();

            foreach (var field in fields)
            {
                var value = field.Layout.Format(log);
                var bsonValue = value as BsonValue ?? BsonValue.Create(value);
                doc.Add(field.Name, bsonValue);
            }
            return doc;
        }

        public async void InsertDocumentInCollection(BsonDocument document, IMongoCollection<BsonDocument> collection)
        {
            await  collection.InsertOneAsync(document);
        }

        public async void InsertDocumentsInCollection(List<BsonDocument> documents, IMongoCollection<BsonDocument> collection)
        {
            await collection.InsertManyAsync(documents);
        }
        
        public BsonDocument BuildBsonDocument(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                return null;
            }

            var toReturn = new BsonDocument {
				{"timestamp", loggingEvent.TimeStamp}, 
				{"level", loggingEvent.Level.ToString()}, 
				{"thread", loggingEvent.ThreadName}, 
				{"userName", loggingEvent.UserName}, 
				{"message", loggingEvent.RenderedMessage}, 
				{"loggerName", loggingEvent.LoggerName}, 
				{"domain", loggingEvent.Domain}, 
				{"machineName", Environment.MachineName}
			};

            // location information, if available
            if (loggingEvent.LocationInformation != null)
            {
                toReturn.Add("fileName", loggingEvent.LocationInformation.FileName);
                toReturn.Add("method", loggingEvent.LocationInformation.MethodName);
                toReturn.Add("lineNumber", loggingEvent.LocationInformation.LineNumber);
                toReturn.Add("className", loggingEvent.LocationInformation.ClassName);
            }

            // exception information
            if (loggingEvent.ExceptionObject != null)
            {
                toReturn.Add("exception", BuildExceptionBsonDocument(loggingEvent.ExceptionObject));
            }

            // properties
            var compositeProperties = loggingEvent.GetProperties();
            if (compositeProperties != null && compositeProperties.Count > 0)
            {
                var properties = new BsonDocument();
                foreach (DictionaryEntry entry in compositeProperties)
                {
                    properties.Add(entry.Key.ToString(), entry.Value.ToString());
                }

                toReturn.Add("properties", properties);
            }

            return toReturn;
        }

        private static BsonDocument BuildExceptionBsonDocument(Exception ex)
        {
            var toReturn = new BsonDocument {
				{"message", ex.Message}, 
				{"source", ex.Source}, 
				{"stackTrace", ex.StackTrace}
			};

            if (ex.InnerException != null)
            {
                toReturn.Add("innerException", BuildExceptionBsonDocument(ex.InnerException));
            }

            return toReturn;
        }
    }
}
