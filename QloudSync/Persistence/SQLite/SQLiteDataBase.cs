using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using System.Windows.Forms;
using System.IO;


namespace GreenQloud.Persistence.SQLite{

    public class SQLiteDatabase
    {
        String dbConnection;

        public SQLiteDatabase(){
        }

        
        public string ConnectionString{
            get{
                return String.Format("URI=file:{0};Version=3;", RuntimeSettings.DatabaseFile);;
            }
        }
        
        public void CreateDataBase(){
            ExecuteNonQuery("CREATE TABLE Repository (RepositoryID INTEGER PRIMARY KEY AUTOINCREMENT , Path ntext)");
            ExecuteNonQuery ("CREATE TABLE RepositoryItem (RepositoryItemID INTEGER PRIMARY KEY AUTOINCREMENT , Name ntext, RelativePath ntext, RepoPath ntext, IsFolder ntext, Deleted ntext)");
            ExecuteNonQuery ("CREATE TABLE EVENT (EventID INTEGER PRIMARY KEY AUTOINCREMENT , ItemId ntext, TYPE ntext, REPOSITORY ntext, SYNCHRONIZED ntext, INSERTTIME ntext)");
            ExecuteNonQuery ("CREATE TABLE TRANSFER (TransferID INTEGER PRIMARY KEY AUTOINCREMENT , ItemId INTEGER, INITIALTIME ntext, ENDTIME ntext, TYPE ntext, STATUS ntext)");
        }


        /// <summary>
        ///     Single Param Constructor for specifying advanced connection options.
        /// </summary>
        /// <param name="connectionOpts">A dictionary containing all desired options and their values</param>
        public SQLiteDatabase(Dictionary<String, String> connectionOpts)
        {
            String str = "";
            foreach (KeyValuePair<String, String> row in connectionOpts)
            {
                str += String.Format("{0}={1}; ", row.Key, row.Value);
            }
            str = str.Trim().Substring(0, str.Length - 1);
            dbConnection = str;
        }
        
        /// <summary>
        ///     Allows the programmer to run a query against the Database.
        /// </summary>
        /// <param name="sql">The SQL to run</param>
        /// <returns>A DataTable containing the result set.</returns>
        public DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                SqliteConnection cnn = new SqliteConnection(ConnectionString);
                cnn.Open();
                SqliteCommand mycommand = new SqliteCommand(cnn);
                mycommand.CommandText = sql;
                SqliteDataReader reader = mycommand.ExecuteReader();
                // Add all the columns.
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    DataColumn col = new DataColumn();
                    col.DataType = reader.GetFieldType(i);
                    col.ColumnName = reader.GetName(i);
                    dt.Columns.Add(col);
                }
                
                while (reader.Read())
                {
                    DataRow row = dt.NewRow();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        // Ignore Null fields.
                        if (reader.IsDBNull(i)) continue;

                        if (reader.GetFieldType(i) == typeof(String))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetString(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Int16))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetInt16(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Int32))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetInt32(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Int64))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetInt64(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Boolean))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetBoolean(i); ;
                        }
                        else if (reader.GetFieldType(i) == typeof(Byte))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetByte(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Char))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetChar(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(DateTime))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetDateTime(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Decimal))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetDecimal(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Double))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetDouble(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(float))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetFloat(i);
                        }
                        else if (reader.GetFieldType(i) == typeof(Guid))
                        {
                            row[dt.Columns[i].ColumnName] = reader.GetGuid(i);
                        }
                    }
                    
                    dt.Rows.Add(row);
                }
                
                reader.Close();
                reader = null;
                mycommand.Dispose();
                mycommand = null;
                cnn.Close();
                cnn = null;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return dt;
        }
        
        /// <summary>
        ///     Allows the programmer to interact with the database for purposes other than a query.
        /// </summary>
        /// <param name="sql">The SQL to be run.</param>
        /// <returns>An Integer containing the number of rows updated.</returns>
        public int ExecuteNonQuery(string sql)
        {
            SqliteConnection cnn = new SqliteConnection(ConnectionString);
            cnn.Open();
            SqliteCommand mycommand = new SqliteCommand(cnn);
            mycommand.CommandText = sql;
            int rowsUpdated = mycommand.ExecuteNonQuery();
            cnn.Close();
            return rowsUpdated;
        }
        
        /// <summary>
        ///     Allows the programmer to retrieve single items from the DB.
        /// </summary>
        /// <param name="sql">The query to run.</param>
        /// <returns>A string.</returns>
        public string ExecuteScalar(string sql)
        {
            SqliteConnection cnn = new SqliteConnection(ConnectionString);
            cnn.Open();
            SqliteCommand mycommand = new SqliteCommand(cnn);
            mycommand.CommandText = sql;
            object value = mycommand.ExecuteScalar();
            cnn.Close();
            if (value != null)
            {
                return value.ToString();
            }
            return "";
        }
        
        /// <summary>
        ///     Allows the programmer to easily update rows in the DB.
        /// </summary>
        /// <param name="tableName">The table to update.</param>
        /// <param name="data">A dictionary containing Column names and their new values.</param>
        /// <param name="where">The where clause for the update statement.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Update(String tableName, Dictionary<String, String> data, String where)
        {
            String vals = "";
            Boolean returnCode = true;
            if (data.Count >= 1)
            {
                foreach (KeyValuePair<String, String> val in data)
                {
                    vals += String.Format(" {0} = '{1}',", val.Key.ToString(), val.Value.ToString());
                }
                vals = vals.Substring(0, vals.Length - 1);
            }
            try
            {
                this.ExecuteNonQuery(String.Format("update {0} set {1} where {2};", tableName, vals, where));
            }
            catch
            {
                returnCode = false;
            }
            return returnCode;
        }
        
        /// <summary>
        ///     Allows the programmer to easily delete rows from the DB.
        /// </summary>
        /// <param name="tableName">The table from which to delete.</param>
        /// <param name="where">The where clause for the delete.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Delete(String tableName, String where)
        {
            Boolean returnCode = true;
            try
            {
                this.ExecuteNonQuery(String.Format("delete from {0} where {1};", tableName, where));
            }
            catch (Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }
        
        /// <summary>
        ///     Allows the programmer to easily insert into the DB
        /// </summary>
        /// <param name="tableName">The table into which we insert the data.</param>
        /// <param name="data">A dictionary containing the column names and data for the insert.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool Insert(String tableName, Dictionary<String, String> data)
        {
            String columns = "";
            String values = "";
            Boolean returnCode = true;
            foreach (KeyValuePair<String, String> val in data)
            {
                columns += String.Format(" {0},", val.Key.ToString());
                values += String.Format(" '{0}',", val.Value);
            }
            columns = columns.Substring(0, columns.Length - 1);
            values = values.Substring(0, values.Length - 1);
            try
            {
                this.ExecuteNonQuery(String.Format("insert into {0}({1}) values({2});", tableName, columns, values));
            }
            catch(Exception fail)
            {
                MessageBox.Show(fail.Message);
                returnCode = false;
            }
            return returnCode;
        }
        
        /// <summary>
        ///     Allows the programmer to easily delete all data from the DB.
        /// </summary>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearDB()
        {
            DataTable tables;
            try
            {
                tables = this.GetDataTable("select NAME from SQLITE_MASTER where type='table' order by NAME;");
                foreach (DataRow table in tables.Rows)
                {
                    this.ClearTable(table["NAME"].ToString());
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        ///     Allows the user to easily clear all data from a specific table.
        /// </summary>
        /// <param name="table">The name of the table to clear.</param>
        /// <returns>A boolean true or false to signify success or failure.</returns>
        public bool ClearTable(String table)
        {
            try
            {
                
                this.ExecuteNonQuery(String.Format("delete from {0};", table));
                return true;
            }
            catch
            {
                return false;
            }
        }       
    }
}

