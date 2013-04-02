using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;
using Mono.Data.Sqlite;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteTransferDAO : TransferDAO
    {
        #region implemented abstract members of TransferDAO

        public override void Create (Transfer transfer)
        {
            string Id = new SQLiteRepositoryItemDAO().GetId(transfer.Item);
            string sql = string.Format("INSERT INTO TRANSFER (ITEMID, INITIALTIME, ENDTIME, TYPE, STATUS) VALUES ({0},\"{1}\",\"{2}\",\"{3}\",\"{4}\")", Id, transfer.InitialTime.ToString(), transfer.EndTime.ToString(), transfer.Type.ToString(), transfer.Status.ToString());
            ExecuteNonQuery (sql);
        }
            
        #endregion

        public bool Exists (Transfer transfer)
        {
            return All.Any (t=> t.EndTime != null);
        }

        public List<Transfer> All{
            get{
                return ExecuteReader("SELECT * FROM TRANSFER");
            }
        }

        public void ExecuteNonQuery (string sqlCommand)
        {
            SqliteConnection conn= new SqliteConnection(SQLiteDatabase.ConnectionString);
            
            Console.WriteLine ("ExecuteNonQuery in TransferDAO");

            SqliteCommand cmd = new SqliteCommand();
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                
                cmd.CommandText = sqlCommand;
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.ExecuteNonQuery();
                
            }
            catch (Exception e){
                Logger.LogInfo ("Database", e);
            }
            finally{
                
                cmd.Dispose();
                conn.Close();
                conn.Dispose();
                conn = null;
                cmd = null;
                GC.Collect();
            }
        }

        public List<Transfer> ExecuteReader(string sqlCommand) {
            List<Transfer> transfers = new List<Transfer>();
            SqliteConnection conn= new SqliteConnection(SQLiteDatabase.ConnectionString);
            SqliteCommand cmd = new SqliteCommand();
            try
            {
                conn.Open();
                cmd = conn.CreateCommand();
                
                cmd.CommandText = sqlCommand;
                cmd.CommandType = System.Data.CommandType.Text;
                SqliteDataReader reader = cmd.ExecuteReader();  
                

                    while (reader.Read()){
                        Transfer transfer = new Transfer();
                        transfer.InitialTime = Convert.ToDateTime (reader.GetString (2));
                        transfer.EndTime = Convert.ToDateTime (reader.GetString (3));
                        transfer.Type = (TransferType) Enum.Parse(typeof(TransferType), reader.GetString(4));
                        transfer.Status = (TransferStatus) Enum.Parse(typeof(TransferStatus), reader.GetString(5));

                        transfers.Add (transfer);
                    }                    
                }
                catch (Exception e){
                    Logger.LogInfo ("Database", e);
                }
                finally{
                    
                    cmd.Dispose();
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                    cmd = null;
                    GC.Collect();
                }
            return transfers;
        }
    }
}


