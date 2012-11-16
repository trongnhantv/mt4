using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;  

namespace UnmanagedExportLibrary3
{
    class Database
    {

        //private SqlConnection myconnection = new SqlConnection("user id=berichadmin;password=berich;server=apps.berich.vn;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;");
        private SqlConnection myconnection = new SqlConnection("user id=sa;password=999999;server=localhost\\SQL;trusted_connection=no;database=forex; connection timeout=90;persist security info=true;user instance=false;");

        public Database()
        {
            this.myconnection.Open();
        }

        public void execute_query(string sql)
        {
            this.log(sql + "\n");
            try
            {
                new SqlCommand(sql, this.myconnection).ExecuteNonQuery();
            }
            catch (Exception e)
            {
                this.log(e.ToString() + "\n" + sql + "\n");
            }
        }

        ~Database()
        {
            this.myconnection.Close();
        }

        public void log(string input)
        {
            System.IO.File.AppendAllText(@"C:\WriteLines.txt", input);
        }

        public DataTable query(string sql)
        {
            SqlCommand mycommand = new SqlCommand(sql, this.myconnection);
            SqlDataAdapter da = new SqlDataAdapter();
            try
            {
                da.SelectCommand = mycommand;
                DataTable dtGet = new DataTable();
                da.Fill(dtGet);
                return dtGet;
            }
            catch (Exception e)
            {
                this.log(e.ToString() + "\n" + sql + "\n");
            }
            return null;
        }
    }
}

