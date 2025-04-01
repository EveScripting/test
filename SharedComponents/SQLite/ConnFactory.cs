/*
 * ---------------------------------------
 * User: duketwo
 * Date: 03.07.2018
 * Time: 12:30
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using EasyHook.IPC;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using SharedComponents.EVE;
using SharedComponents.EVE.DatabaseSchemas;
using SharedComponents.Extensions;

namespace SharedComponents.SQLite
{
    public class ConnFactory
    {
        public static readonly ConnFactory Instance = new ConnFactory();

        internal OrmLiteConnectionFactory Factory { get; private set; }

        static ConnFactory()
        {
            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
            using (var wc = WriteConn.Open())
            {

                // delete a table

                //if (wc.DB.TableExists<AbyssStatEntry>())
                //{
                //    wc.DB.DropTable<AbyssStatEntry>();
                //}

                // rename a table
                //if (wc.DB.TableExists("StatisticsEntryCSV") && !wc.DB.TableExists("StatisticsEntry"))
                //{
                //    Console.WriteLine("Altering db table StatisticsEntryCSV name to StatisticsEntry.");
                //    wc.DB.ExecuteSql("ALTER TABLE StatisticsEntryCSV RENAME TO StatisticsEntry");
                //}

                //create tables
                //wc.DB.CreateTableIfNotExists<StatisticsEntry>();
                //wc.DB.CreateTableIfNotExists<AbyssStatEntry>();
                //wc.DB.CreateTableIfNotExists<CachedWebsiteEntry>();
                //wc.DB.CreateTableIfNotExists<AbyssHunterVisited>();
                //wc.DB.CreateTableIfNotExists<AbyssHunterScans>();
                //wc.DB.CreateTableIfNotExists<GateCampCheckEntry>();

                EnsureTable<StatisticsEntry>(wc.DB);
                EnsureTable<AbyssStatEntry>(wc.DB);
                EnsureTable<CachedWebsiteEntry>(wc.DB);
                EnsureTable<AbyssHunterVisited>(wc.DB);
                EnsureTable<AbyssHunterScans>(wc.DB);
                EnsureTable<GateCampCheckEntry>(wc.DB);


                //if (wc.DB.TableExists<StatisticsEntry>())
                //{
                //    // delete columns
                //    if (wc.DB.ColumnExists("StatisticsEntry", "Test"))
                //    {

                //    }

                //    // add missing columns
                //    if (!wc.DB.ColumnExists("StatisticsEntry", "Test"))
                //    {

                //    }
                //}

                //if (wc.DB.TableExists<AbyssStatEntry>())
                //{
                //    // Check if the column exists, and if not, add the column
                //    if (!wc.DB.ColumnExists<AbyssStatEntry>(x => x.AStarErrors))
                //    {
                //        // Add the missing column 'AStarErrors' of type 'int'
                //        wc.DB.AddColumn<AbyssStatEntry>(c => c.AStarErrors);
                //    }
                //}
            }
        }


        private ConnFactory()
        {
            var connString = ConnectionString;
            Console.WriteLine($"connString: {connString}");
            Factory = new OrmLiteConnectionFactory(connString, SqliteDialect.Provider);

        }

        private static void EnsureTable<T>(IDbConnection db)
        {
            Debug.WriteLine($"Table of type {typeof(T)} was ensured.");
            db.CreateTableIfNotExists<T>();
            EnsureColumns<T>(db);
        }

        private static void EnsureColumns<T>(IDbConnection db)
        {
            var modelDef = typeof(T).GetModelMetadata();
            var existingColumns = GetColumnNames(db, modelDef.ModelName);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            var modelColumns = properties
                .Select(p => OrmLiteConfig.DialectProvider.NamingStrategy.GetColumnName(p.Name))
                .ToList();

            // Add missing columns
            foreach (var property in properties)
            {
                var columnName = OrmLiteConfig.DialectProvider.NamingStrategy.GetColumnName(property.Name);
                var columnType = GetSqlColumnType(property.PropertyType);

                if (!existingColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase))
                {
                    var addColumnSql = $"ALTER TABLE {modelDef.ModelName} ADD COLUMN {columnName} {columnType}";
                    Debug.WriteLine(addColumnSql);
                    db.ExecuteSql(addColumnSql);
                }
            }

            // Remove obsolete columns
            foreach (var existingColumn in existingColumns)
            {
                if (!modelColumns.Contains(existingColumn, StringComparer.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Model columns do not include [{existingColumn}], removing [{existingColumn}]");
                    DropColumn(db, modelDef.ModelName, existingColumn);
                }
            }
        }

        private static List<string> GetColumnNames(IDbConnection db, string tableName)
        {
            var columnNames = new List<string>();
            var query = $"PRAGMA table_info({tableName})";
            var result = db.SqlList<dynamic>(query);

            foreach (var row in result)
            {
                columnNames.Add(row.name.ToString());
            }

            return columnNames;
        }

        private static string GetSqlColumnType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return "INTEGER";
            if (type == typeof(long) || type == typeof(long?))
                return "INTEGER";
            if (type == typeof(bool) || type == typeof(bool?))
                return "BOOLEAN";
            if (type == typeof(double) || type == typeof(double?))
                return "REAL";
            if (type == typeof(float) || type == typeof(float?))
                return "REAL";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "DATETIME";
            if (type == typeof(decimal) || type == typeof(decimal?))
                return "NUMERIC";
            if (type == typeof(string))
                return "TEXT";
            if (type == typeof(byte[]))
                return "BLOB";

            throw new NotSupportedException($"The type '{type.Name}' is not supported.");
        }

        private static void DropColumn(IDbConnection db, string tableName, string columnName)
        {
            // SQLite does not support dropping columns directly. We need to recreate the table.
            var tempTableName = tableName + "_temp";
            var backupTableName = tableName + "_backup_" + DateTime.UtcNow.ToUnixTime();
            var columnDefinitions = GetColumnDefinitions(db, tableName)
                .Where(c => !c.StartsWith($"\"{columnName}\" ", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!columnDefinitions.Any())
                return;

            //CREATE TABLE Table_backup AS SELECT* FROM Table

            var columns = string.Join(", ", columnDefinitions);
            var copyColumns = string.Join(", ", columnDefinitions.Select(c => c.Split(' ')[0]));

            var createBackupTableSql = $"CREATE TABLE {backupTableName} AS SELECT * FROM {tableName}";
            var createTempTableSql = $"CREATE TABLE {tempTableName} ({columns})";
            var copyDataSql = $"INSERT INTO {tempTableName} SELECT {copyColumns} FROM {tableName}";
            var dropTableSql = $"DROP TABLE {tableName}";
            var renameTableSql = $"ALTER TABLE {tempTableName} RENAME TO {tableName}";

            var trans = db.BeginTransaction();

            try
            {
                db.ExecuteSql(createBackupTableSql);
                db.ExecuteSql(createTempTableSql);
                db.ExecuteSql(copyDataSql);
                db.ExecuteSql(dropTableSql);
                db.ExecuteSql(renameTableSql);
                trans.Commit();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                trans.Rollback();

            }
            finally
            {
                trans.Dispose();
            }
        }

        private static List<string> GetColumnDefinitions(IDbConnection db, string tableName)
        {
            var columnDefinitions = new List<string>();
            var query = $"PRAGMA table_info({tableName})";
            var result = db.SqlList<dynamic>(query);

            foreach (var row in result)
            {
                columnDefinitions.Add($"\"{row.name}\" {row.type}");
            }

            return columnDefinitions;
        }

        private static void RenameTableIfExists(IDbConnection db, string oldName, string newName)
        {
            if (db.TableExists(oldName) && !db.TableExists(newName))
            {
                var renameTableSql = $"ALTER TABLE {oldName} RENAME TO {newName}";
                db.ExecuteSql(renameTableSql);
            }
        }

        public IDbConnection OpenWriteConn()
        {
            return Factory.OpenDbConnection();
        }

        public static IDbConnection Open()
        {
            return Instance.OpenWriteConn();
        }

        private string ConnectionString
        {
            get
            {
                var _dbFileName = Path.Combine(Utility.Util.AssemblyPath, "EVESharpSettings", "DB.SQLite");
                SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
                builder.DataSource = _dbFileName;
                builder.Pooling = true;
                builder.SyncMode = SynchronizationModes.Normal;
                builder.FailIfMissing = false;
                builder.Version = 3;
                builder.JournalMode = SQLiteJournalModeEnum.Wal;
                return builder.ToString();
            }
        }
    }

}
