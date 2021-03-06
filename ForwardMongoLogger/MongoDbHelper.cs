﻿namespace ForwardMongoLogger
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using log4net.Core;
    public class MongoDbHelper
    {
        #region Constants

        public static readonly string CollectionDefaulName = "logs";
        public static readonly string DataBaseDefaulName = "MongoLog";
        public static readonly long DefaultCappedCollectionSize = 4096;
        public static readonly long DefaultCappedCollectionMaxDocuments = 2000;

        #endregion

        #region Public Members

        public IMongoCollection<BsonDocument> GetCollection(IMongoDatabase mongoDatabase, bool? cappedCollection,
            string collectionName, long? cappedCollectionSize, long? maxNumberOfDocuments)
        {
            IMongoCollection<BsonDocument> retVal = null;

            if (cappedCollection == null || !cappedCollection.Value)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }
            
            if (!IsCollectionExistsAsync(mongoDatabase, collectionName).Result)
            {
                CreateCollectionAsync(mongoDatabase, collectionName, cappedCollectionSize, maxNumberOfDocuments).Wait();
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }

            if (IsCappedCollection(collectionName, mongoDatabase).Result)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
                return retVal;
            }


            if (ConvertCollectionToCapped(collectionName, mongoDatabase, cappedCollectionSize, maxNumberOfDocuments).Result)
            {
                retVal = mongoDatabase.GetCollection<BsonDocument>(collectionName ?? CollectionDefaulName);
            }
            
            return retVal;
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

        public BsonDocument BuildBsonDocument(LoggingEvent loggingEvent, List<MongoAppenderFileld> fields)
        {
            var doc = new BsonDocument();
            foreach (MongoAppenderFileld field in fields)
            {
                object value = field.Layout.Format(loggingEvent);
                var bsonValue = value as BsonValue ?? BsonValue.Create(value);
                doc.Add(field.Name, bsonValue);
            }
            return doc;

        }
        
        #endregion

        #region Private Members

        private static async Task<bool> IsCappedCollection(string collectionName, IMongoDatabase mongoDatabase)
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                {"collstats", collectionName}
            });

            var stats = await mongoDatabase.RunCommandAsync(command);
            return stats["capped"].AsBoolean;
        }

        private static async Task<bool> ConvertCollectionToCapped(string collectionName, IMongoDatabase mongoDatabase, long? cappedCollectionSize, long? maxNumberOfDocuments)
        {
            var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                {"convertToCapped", collectionName},
                {"size",cappedCollectionSize ?? DefaultCappedCollectionSize },
                {"max",maxNumberOfDocuments ?? DefaultCappedCollectionMaxDocuments}
            });

            await mongoDatabase.RunCommandAsync(command);
            return await IsCappedCollection(collectionName, mongoDatabase);
        }

        private static async Task CreateCollectionAsync(IMongoDatabase mongoDatabase, string collectionName, long? cappedCollectionSize, long? maxNumberOfDocuments)
        {
            var createCollectionOptions = new CreateCollectionOptions()
            {
                Capped = true,
                MaxSize = cappedCollectionSize ?? DefaultCappedCollectionSize,
                MaxDocuments = maxNumberOfDocuments ?? DefaultCappedCollectionMaxDocuments
            };

            await mongoDatabase.CreateCollectionAsync(collectionName ?? CollectionDefaulName, createCollectionOptions);
        }
        
        #endregion
    }
}
