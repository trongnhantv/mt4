using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
namespace UnmanagedExportLibrary3
{
    internal static class UnmanagedExports
    {
        [DllExport("getOpen", CallingConvention = CallingConvention.StdCall)]
        public static string getOpen()
        {
            try
            {
                Database db = new Database();
                DataTable dt = db.query("select stock_id,portfolio_id,type,volume,avgPrice,rec,date from stockinportfolio where rec like 'OPEN%';");               
                string order = "";
                foreach (DataRow Row in dt.Rows)
                {
                    order += Row["stock_id"].ToString() + ",";
                    order += Row["portfolio_id"].ToString() + ",";
                    order += Row["type"].ToString() + ",";
                    order += Row["volume"].ToString() + ",";
                    order += Row["avgPrice"].ToString() + ",";
                    order += Row["rec"].ToString() + ",";
                    order += Row["date"].ToString();
                    order += ";";
                }
                if (order.Length == 0)
                    return "";
                else
                    return order.Substring(0, order.Length - 1);
            }
            catch (Exception e)
            {
                log(e.ToString());
                return "";
            }
        }
       
        [DllExport("getClose", CallingConvention = CallingConvention.StdCall)]
        public static string getClose()
        {
            try
            {
                Database db = new Database();
                DataTable dt = db.query(string.Format("select ticket,P.stock_id,S.portfolio_id,S.type from StockInPortfolio as S join PositionDetails as P on S.stock_id = P.stock_id and S.portfolio_id=P.portfolio_id and S.type = P.type where S.rec like '%CLOSE%'"));
                //get string of closed positions
                string order = "";
                foreach (DataRow row in dt.Rows)
                {
                    order += row["ticket"].ToString() + ";";
                }
                if (order.Length == 0)
                    return "";
                else
                    return order.Substring(0, order.Length - 1);
            }
            catch (Exception e)
            {
                log(e.ToString());
                return "";
            }
        }
        //delete all pending order and record
        [DllExport("cleanupPending", CallingConvention = CallingConvention.StdCall)]
        public static void cleanupPending()
        {
            try
            {

                Database db = new Database();
                db.executeNonQuery("delete from pendingDetails");
                db.executeNonQuery("delete from Stockinportfolio where rec like '%PENDING%';");
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        //  get tickets of pending orders
        //get all information of pending orders of previous hour. Then cleanup this table.
        [DllExport("getPendingDetails", CallingConvention = CallingConvention.StdCall)]
        public static string getPendingDetails()
        {
            try
            {
                Database db = new Database();
                DataTable table = db.query("select ticket,stock_id,portfolio_id,type from pendingDetails");
                string orders = "";
                foreach (DataRow row in table.Rows)
                {
                    orders += row["ticket"] + ";";
                }
                if (orders.Length == 0)
                    return "";
                else
                    //db.execute_query("delete from pendingDetails");
                    return orders.Substring(0, orders.Length - 1);
            }
            catch (Exception e)
            {
                log(e.ToString());
                return "";
            }
        }

        //check if pending orders are open
        //if pending ordre is open, change it to open( using updateOpen() tweak it to change from Pending to Open)
         //else, delete the pending
        [DllExport("updatePending", CallingConvention = CallingConvention.StdCall)]
        public static void updatePending( int ticket, string order_date,double price)
        {
            try
            {
                //TODO
                Database db = new Database();
                //extract all information from the ticket and pass to update Open
                DataTable dt = db.query(string.Format("select stock_id,portfolio_id,volume,date,type from Stockinportfolio where stock_ticket ={0}", ticket));
                string stock_id = dt.Rows[0]["stock_id"].ToString();
                int portfolio_id = Int32.Parse(dt.Rows[0]["portfolio_id"].ToString());
                double volume = Int32.Parse(dt.Rows[0]["volume"].ToString());
                string rec_date = dt.Rows[0]["date"].ToString();
                string type = dt.Rows[0]["type"].ToString();
                //change status from PENDING to ''
                db.executeNonQuery("update Stockinportfolio set rec ='OPEN' where stock_ticket=" + ticket);
                log(stock_id + " " + portfolio_id + " " + ticket + " " + volume + " " + price + " " + order_date + " " + rec_date + " " + type + "\n\n");
                updateOpenLocal(stock_id, portfolio_id, ticket, volume, price, order_date, rec_date, "OPEN", type);
            }
            catch (System.Exception ex)
            {
                log(ex.ToString());
            }
            
            //updateOpen(stock_id, portfolio_id, ticket, volume, price, order_date, rec_date, "OPEN", type);
            //updateOpen(string stock_id, int portfolio_id, int ticket, int volume, double price, string order_date, string rec_date, string rec, string type)
            //delete the pending order by ticket
            //change pending order in stockinportfolio into open      
        
            return ;
        }

        //update the ticket and the record
        [DllExport("updateOpen", CallingConvention = CallingConvention.StdCall)]
        public static void updateOpen(string stock_id, int portfolio_id, int ticket, double volume, double price, string order_date, string rec_date, string rec, string type)
        {
            //set up connection
            SqlConnection myConnection = new SqlConnection(Database.getCString());
            string sql = "";
            try
            {
                myConnection.Open();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            try
            {
                
                //if ticket =-1 aka position is NOT successfully opened then just set stock_ticket=-1 in StockRecommend table
                if (ticket < 0)
                {
                    sql += string.Format("update StockRecommend set stock_ticket = {0} where  portfolio_id={1} and stock_id='{2}' and date ='{3}' and type ='{4}'",
                        ticket,portfolio_id, stock_id, rec_date, type);
                    log(sql);
                }
                //else
                //1. insert order into Stock record
                //2. Create new record in PositionDetails
                //2. make new record in stockStockInPortfolio with real price date and volume
                //if the currency exist then stack up the volume, and update the new price

                // 3. update the stock ticket in Stockrecommended  
                else
                {
                    //1. insert order into stock Record
                    sql += string.Format("insert into StockRecord(portfolio_id,stock_id,stock_ticket,date,volume,price,status) values({0},'{1}',{2},'{3}',{4},{5},'{6}');",
                    portfolio_id, stock_id, ticket, order_date, volume, price, type);

                    //2. Create new record in PositionDetails
                    sql += string.Format("insert into PositionDetails(portfolio_id,stock_id,type,volume,price,ticket) values ('{0}','{1}','{2}',{3},{4},{5});",
                    portfolio_id, stock_id, type, volume, price, ticket);

                    //3.make new record in stockStockInPortfolio with real price date and volume
                    //check if the current position  exists in the portfolio
                    SqlDataReader myreader = null;
                    //date is excluded because there cannot exist two records of the same type in Stockinportfolio
                    string query = string.Format("select volume,avgprice from StockInPortfolio where portfolio_id={0} and stock_id='{1}' and type ='{2}' and rec ='';", portfolio_id, stock_id, type);
                    SqlCommand mycommand = new SqlCommand(query, myConnection);
                    myreader = mycommand.ExecuteReader();

                    //if exist stack up the volume and update the price
                    if (myreader.Read())
                    {
                        int old_volume = int.Parse(myreader["volume"].ToString());
                        double old_price = Double.Parse(myreader["avgprice"].ToString());
                        //calculate new price and volume
                        double new_volume = old_volume + volume;
                        double new_price = (price * volume + old_price * old_volume) / new_volume;
                        sql += string.Format("update StockInPortfolio set volume={0}, avgprice={1} where portfolio_id={2} and stock_id='{3}' and type like '%{4}%' and rec ='';", new_volume, new_price, portfolio_id, stock_id, type);
                    }

                    //else, create new record
                    else
                    {
                        sql += string.Format("insert into StockInPortfolio(stock_id,portfolio_id,type,volume,date,avgPrice,rec)  values('{0}',{1},'{2}',{3},'{4}',{5},'');",
                            stock_id, portfolio_id, type, volume, order_date, price);
                    }
                    myreader.Close();
                    //4. update the stock ticket in Stockrecommended
                    sql += string.Format("update StockRecommend set stock_ticket+='{0}'+',' , rec ='{1}' where  portfolio_id={2} and stock_id like '{3}' and date = '{4}';",
                    ticket, rec, portfolio_id, stock_id, rec_date);
                }
                log(sql);
                SqlCommand myCommand = new SqlCommand(sql, myConnection);
                myCommand.ExecuteNonQuery();
                myConnection.Close();

            }
            catch (System.Exception e)
            {
                log(e.ToString() + "\n\n"+sql);
            }


        }

        
        //Store currently pending order into
        [DllExport("storePending", CallingConvention = CallingConvention.StdCall)]
        public static void storePending(string stock_id, int portfolio_id, int ticket, double volume, double price, string order_date, string rec_date, string rec, string type)
        {
            try
            {
                //convert volume back to stock scale
                volume *= 100;
                Database db = new Database();
                //if ticket =-1 aka position is NOT successfully opened then just set stock_ticket=-1 in StockRecommend table
                if (ticket < 0)
                {
                    db.executeNonQuery(string.Format("update StockRecommend set stock_ticket = {0} where  portfolio_id={1} and stock_id='{2}' and date ='{3}' and type ='{4}'",
                            ticket, portfolio_id, stock_id, rec_date, type));
                    return;
                }
                //else
                //1. change rec in Stockinportfolio to pending
                //2. create new record in pendingDetails             
                //3. change stock recommend status to pending
                else
                {
                    //1. change rec in Stockinportfolio to pending
                    db.executeNonQuery(string.Format("update StockInPortfolio set rec = 'PENDING' , stock_ticket={0} where portfolio_id={1} and stock_id='{2}' and type like '%{3}%' and rec like '%OPEN%';", ticket, portfolio_id, stock_id, type));
                    //2. create new record in pendingDetails  
                    db.executeNonQuery(string.Format("insert into PendingDetails(portfolio_id,stock_id,type,volume,price,ticket) values ('{0}','{1}','{2}',{3},{4},{5});",
                        portfolio_id, stock_id, type, volume, price, ticket));
                    //3. change stock recommend status to pending
                    db.executeNonQuery(string.Format("update StockRecommend set stock_ticket='PENDING', rec ='{0}' where  portfolio_id={1} and stock_id like '{2}' and date = '{3}';",
                        rec, portfolio_id, stock_id, rec_date));
                    return;
                }
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }
        //return tickets of opening Position to check if it were autoclosed or not.
        [DllExport("getAutoClose", CallingConvention = CallingConvention.StdCall)]
        public static string getAutoClose()
        {
            try
            {
                string tickets = "";
                string stock_id = "";
                string portfolio_id = "";
                string type = "";

                //return a list of closed position
                Database db = new Database();
                DataTable table = db.query("select stock_id,portfolio_id,type from StockInPortfolio where rec = ''");
                //query the ticket
                if (table.Rows.Count == 0) return "";
                DataTable ticket_table;
                foreach (DataRow row in table.Rows)
                {

                    //make the select ticket query
                    stock_id = row["stock_id"].ToString();
                    portfolio_id = row["portfolio_id"].ToString();
                    type = row["type"].ToString();
                    ticket_table = db.query(String.Format("select ticket from PositionDetails where stock_id='{0}' and portfolio_id={1} and type = '{2}' ", stock_id, portfolio_id, type));

                    //prepare the tickets string
                    if (ticket_table.Rows.Count == 0)
                    {
                        return "error";
                    }
                    foreach (DataRow ticket_row in ticket_table.Rows)
                    {
                        tickets += ticket_row["ticket"].ToString() + ";";
                    }
                }
                if (tickets.Length == 0)
                    return "";
                return tickets.Substring(0, tickets.Length - 1);
            }
            catch (Exception e)
            {
                log(e.ToString());
                return "";
            }
        }


        //cleanup all the orphan position
        [DllExport("cleanClose", CallingConvention = CallingConvention.StdCall)]
        public static void cleanClose()
        {
            try
            {
                Database db = new Database();
                string sql = "delete from StockInPortfolio where (rec ='' or rec like'%CLOSE%') and (select COUNT(P.ticket) from PositionDetails as P where StockInPortfolio.stock_id=P.stock_id and StockInPortfolio.portfolio_id=p.portfolio_id and StockInPortfolio.type=p.type) = 0";
                db.executeNonQuery(sql);
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        //Update PendingDetails and Stockinportfolio when a position has been closed
        public static void updateClose(int ticket, string date, double volume, double price)
        {
            
            Database db = new Database();
            string sql = "";
            DataTable temp;
            try
            {
                //extract information from PositionDetails
                temp = db.query(String.Format("select portfolio_id,stock_id,type from PositionDetails where ticket ={0}",ticket));
                temp.Rows[0]["portfolio_id"].ToString();
                string portfolio_id = temp.Rows[0]["portfolio_id"].ToString();
                string stock_id = temp.Rows[0]["stock_id"].ToString();
                string status = temp.Rows[0]["type"].ToString();
                //delete the record in PositionsDetails
                string delete = string.Format("delete from PositionDetails where ticket={0};", ticket);   
                db.executeNonQuery(delete);
                //update volume and price and volume in Stockinportfolio to reflect the change                        
                 
                //if the last record is reach, no update is needed because the record in Stockinportfolio will be deleted eventually
                string nvolume = db.query("select SUM(volume) as volume from PositionDetails").Rows[0]["volume"].ToString();
                if (nvolume.Length>0)
                {
                    int new_volume = Int32.Parse(nvolume);
                    double new_price = Double.Parse(db.query("select SUM(volume*price)/SUM(volume) as avgPrice from PositionDetails").Rows[0]["avgPrice"].ToString());
                
                
                 //update new volume and price
                sql += string.Format("update Stockinportfolio set volume={3} , avgPrice = {4} where portfolio_id = {0} AND stock_id = '{1}' AND type = '{2}' and rec ='' ;",portfolio_id, stock_id
                        ,status, new_volume, new_price);
               
                }

                //insert in StockRecord                        
                sql += String.Format("insert into stockrecord(portfolio_id,stock_id,stock_ticket,date,volume,price,status) values({0},'{1}',{2},'{3}',{4},{5},'{6}');",
                       portfolio_id, stock_id, ticket, date, -volume, price, status);
                //log(sql.Replace(";","\n") + "\n");
                db.executeNonQuery(sql);

            }
            catch (System.Exception e)
            {
                log(e.ToString() + "\n" + sql);
            }
            
        }

        [DllExport("updateCash", CallingConvention = CallingConvention.StdCall)]
         public static void updateCash(int portfolio_id, double cash, double capital)
        {
            try
            {
                Database db = new Database();
                string sql = string.Format("update Portfolio set cash={0},capital={1} where portfolio_id={2};", cash, capital, portfolio_id);
                db.executeNonQuery(sql);
            }
            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        [DllExport("updatePrice", CallingConvention = CallingConvention.StdCall)]
        public static void updatePrice(string symbol, string date_time, double open, double close, double high, double low, int volume)
        {
            try
            {
                Database db = new Database();
                log(string.Format("insert into Stock(stock_id,date,openPrice, closedPrice,maxPrice,minPrice,matchedVolume ) values('{0}','{1}',{2},{3},{4},{5},{6});", symbol, date_time,
                    open, close, high, low, volume));
                db.executeNonQuery(string.Format("insert into Stock(stock_id,date,openPrice, closedPrice,maxPrice,minPrice,matchedVolume ) values('{0}','{1}',{2},{3},{4},{5},{6});", symbol, date_time,
                    open, close, high, low, volume));
            }

            catch (Exception e)
            {
                log(e.ToString());
            }
        }

        //log the error
        public static void log(string input)
        {
            System.IO.File.AppendAllText(@"C:\error.txt", input);
        }

        public static void updateOpenLocal(string stock_id, int portfolio_id, int ticket, double volume, double price, string order_date, string rec_date, string rec, string type)
        {
            //set up connection
            SqlConnection myConnection = new SqlConnection(Database.getCString());
            string sql = "";
            try
            {
                myConnection.Open();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            try
            {

                //if ticket =-1 aka position is NOT successfully opened then just set stock_ticket=-1 in StockRecommend table
                if (ticket < 0)
                {
                    sql += string.Format("update StockRecommend set stock_ticket = {0} where  portfolio_id={1} and stock_id='{2}' and date ='{3}' and type ='{4}'",
                        ticket, portfolio_id, stock_id, rec_date, type);
                    log(sql);
                }
                //else
                //1. insert order into Stock record
                //2. Create new record in PositionDetails
                //2. make new record in stockStockInPortfolio with real price date and volume
                //if the currency exist then stack up the volume, and update the new price

                // 3. update the stock ticket in Stockrecommended  
                else
                {
                    //1. insert order into stock Record
                    sql += string.Format("insert into StockRecord(portfolio_id,stock_id,stock_ticket,date,volume,price,status) values({0},'{1}',{2},'{3}',{4},{5},'{6}');",
                    portfolio_id, stock_id, ticket, order_date, volume, price, type);

                    //2. Create new record in PositionDetails
                    sql += string.Format("insert into PositionDetails(portfolio_id,stock_id,type,volume,price,ticket) values ('{0}','{1}','{2}',{3},{4},{5});",
                    portfolio_id, stock_id, type, volume, price, ticket);

                    //3.make new record in stockStockInPortfolio with real price date and volume
                    //check if the current position  exists in the portfolio
                    SqlDataReader myreader = null;
                    //date is excluded because there cannot exist two records of the same type in Stockinportfolio
                    string query = string.Format("select volume,avgprice from StockInPortfolio where portfolio_id={0} and stock_id='{1}' and type ='{2}' and rec ='';", portfolio_id, stock_id, type);
                    SqlCommand mycommand = new SqlCommand(query, myConnection);
                    myreader = mycommand.ExecuteReader();

                    //if exist stack up the volume and update the price
                    if (myreader.Read())
                    {
                        int old_volume = int.Parse(myreader["volume"].ToString());
                        double old_price = Double.Parse(myreader["avgprice"].ToString());
                        //calculate new price and volume
                        double new_volume = old_volume + volume;
                        double new_price = (price * volume + old_price * old_volume) / new_volume;
                        sql += string.Format("update StockInPortfolio set volume={0}, avgprice={1} where portfolio_id={2} and stock_id='{3}' and type like '%{4}%' and rec ='';", new_volume, new_price, portfolio_id, stock_id, type);
                    }

                    //else, create new record
                    else
                    {
                        sql += string.Format("insert into StockInPortfolio(stock_id,portfolio_id,type,volume,date,avgPrice,rec)  values('{0}',{1},'{2}',{3},'{4}',{5},'');",
                            stock_id, portfolio_id, type, volume, order_date, price);
                    }
                    myreader.Close();
                    //4. update the stock ticket in Stockrecommended
                    sql += string.Format("update StockRecommend set stock_ticket+='{0}'+',' , rec ='{1}' where  portfolio_id={2} and stock_id like '{3}' and date = '{4}';",
                    ticket, rec, portfolio_id, stock_id, rec_date);
                }
                log(sql);
                SqlCommand myCommand = new SqlCommand(sql, myConnection);
                myCommand.ExecuteNonQuery();
                Database db = new Database();
                db.executeNonQuery(string.Format("delete from Stockinportfolio where portfolio_id={0} and stock_id='{1}' and type like '%{2}%' and rec like '%OPEN%'", portfolio_id, stock_id, type));
                myConnection.Close();

            }
            catch (System.Exception e)
            {
                log(e.ToString() + "\n\n" + sql);
            }


        }

    }   
}
