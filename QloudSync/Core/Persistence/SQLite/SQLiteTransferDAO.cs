using System;
using System.Collections.Generic;
using GreenQloud.Repository;
using System.Linq;
using System.Xml;
using System.Threading;
using GreenQloud.Model;
using Mono.Data.Sqlite;
using System.Data;

namespace GreenQloud.Persistence.SQLite
{
    public class SQLiteTransferDAO : TransferDAO
    {
        #region implemented abstract members of TransferDAO
        SQLiteDatabase database = new SQLiteDatabase();
        public override void Create (Transfer transfer)
        {
            string Id = new SQLiteRepositoryItemDAO().GetId(transfer.Item);
            string sql = string.Format("INSERT INTO TRANSFER (ITEMID, INITIALTIME, ENDTIME, TYPE, STATUS) VALUES ({0},\"{1}\",\"{2}\",\"{3}\",\"{4}\")", Id, transfer.InitialTime.ToString(), transfer.EndTime.ToString(), transfer.Type.ToString(), transfer.Status.ToString());
            database.ExecuteNonQuery (sql);
        }
            
        #endregion

        public bool Exists (Transfer transfer)
        {
            return All.Any (t=> t.EndTime != null);
        }

        public List<Transfer> All{
            get{
                return Select("SELECT * FROM TRANSFER");
            }
        }

        public List<Transfer> Select (string sql){
            List<Transfer> transfers = new List<Transfer>();
            DataTable dt = database.GetDataTable(sql);
            foreach(DataRow dr in dt.Rows){
                Transfer transfer = new Transfer();
                transfer.InitialTime = Convert.ToDateTime (dr[2].ToString ());
                transfer.EndTime = Convert.ToDateTime (dr[3].ToString ());
                transfer.Type = (TransferType) Enum.Parse(typeof(TransferType), dr[4].ToString());
                transfer.Status = (TransferStatus) Enum.Parse(typeof(TransferStatus), dr[5].ToString());

                transfers.Add (transfer);
            }
            return transfers;
        }
    }
}


