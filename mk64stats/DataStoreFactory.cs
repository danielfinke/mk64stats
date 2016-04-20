using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mk64stats
{
    class DataStoreFactory
    {
        public static IDataStore New()
        {
            switch (Properties.Settings.Default.DatabaseMode)
            {
                case "sqlite":
                    DataStores.SQLite db = new DataStores.SQLite(Properties.Settings.Default.ConnectionString);
                    return db;
                case "file":
                    DataStores.FlatFile file = new DataStores.FlatFile(Properties.Settings.Default.DataFilePath);
                    return file;
                default:
                    throw new DataStoreFactoryException("Database not configured");
            }
        }

        public class DataStoreFactoryException : Exception
        {
            public DataStoreFactoryException(string message) : base(message)
            {
                
            }
        }
    }
}
