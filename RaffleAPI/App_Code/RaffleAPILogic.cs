using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for LotDBLogic
/// </summary>

namespace Bala.Raffle
{
    public class RaffleAPILogic
    {
        System.Collections.Specialized.NameValueCollection parameters;
        HttpResponse pageResponse;
        Guid objectGuid;
        string name;
        int durationInMinutes;
        enum RaffleStatus
        {
            Active, InActive, ActiveClosed, Unregistered
        }

        public RaffleAPILogic(System.Collections.Specialized.NameValueCollection _parameters, HttpResponse _pageResponse)
        {
            parameters = _parameters;
            pageResponse = _pageResponse;
        }

        public void HandlePageLoad()
        {
            try
            {
                // Get the object GUID
                if (parameters["objectguid"] != null && parameters["objectguid"].ToString() != String.Empty)
                {
                    objectGuid = Guid.Parse(parameters["objectguid"].ToString());
                }
                else
                {
                    pageResponse.Write("001");
                    return;
                }

                // Validate the GUID and if it does not exist create one
                this.validateObjectGUID();

                if (parameters["action"] != null && parameters["action"].ToString() != String.Empty)
                {
                    if (parameters["action"].ToString() == "isactive")
                    {
                        this.isActive();
                    }

                    else if (parameters["action"].ToString() == "startnewdraw")
                    {
                        durationInMinutes = Int32.Parse(parameters["duration"].ToString());
                        this.startNewDraw();
                    }
                    else if (parameters["action"].ToString() == "add")
                    {
                        name = parameters["name"].ToString();
                        this.addContestant();
                    }
                    else if (parameters["action"].ToString() == "check")
                    {
                        name = parameters["name"].ToString();

                        this.check();
                    }
                    else if (parameters["action"].ToString() == "draw")
                    {
                        this.draw();
                    }
                    else if (parameters["action"].ToString() == "close")
                    {
                        this.closeRaffle();
                    }
                }
            }
            catch (Exception ex)
            {
                pageResponse.Write(ex.ToString());
            }
        }

        private Boolean validateObjectGUID()
        {
            string sqlQueryRead = "select * from LotObjectsInWorld where ObjectGuid = '" + objectGuid + "'";
            string sqlQueryInsert = "insert into LotObjectsInWorld(ObjectGuid) values('" + objectGuid + "')";

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = sqlQueryRead;     // Set CommandText to our query that will create the table

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Ok the record exists
                            //pageResponse.Write("GUID EXISTS ALREADY");
                            con.Close();
                            return true;
                        }
                    }

                    // Ok the record does not exist
                    // Insert the record here
                    com.CommandText = sqlQueryInsert;
                    com.ExecuteNonQuery();
                    con.Close();
                }
            }

            //pageResponse.Write("GUID CREATED");

            return true;
        }

        private RaffleStatus isActive()
        {
            RaffleStatus status = RaffleStatus.Active;
            string sqlQueryRead = "select * from LotObjectsInWorld where ObjectGuid = '" + objectGuid + "'";
            Boolean objectRegistered = false;

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = sqlQueryRead;     // Set CommandText to our query that will create the table

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            objectRegistered = true;
                            if (reader["LotDrawNumber"] != System.DBNull.Value && reader["LotDrawNumber"].ToString() != string.Empty && Int32.Parse(reader["LotDrawNumber"].ToString()) != 0)
                            {
                                string sqlReadDuration = "select * from LotDrawHistory where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + reader["LotDrawNumber"].ToString() + "'";
                                reader.Close();

                                com.CommandText = sqlReadDuration;
                                using (System.Data.SQLite.SQLiteDataReader reader1 = com.ExecuteReader())
                                {

                                    if (reader1.Read())
                                    {
                                        // check for duration here
                                        int duration = Int32.Parse(reader1["Duration"].ToString());
                                        DateTime LotStartDateTime = DateTime.Parse(reader1["LotStartDateTime"].ToString());

                                        if (duration != -1 && DateTime.Now > LotStartDateTime.AddMinutes(duration) )
                                        {
                                            pageResponse.Write("ACTIVEBUTCLOSED");
                                            status = RaffleStatus.ActiveClosed;
                                        }
                                        else
                                        {
                                            pageResponse.Write("ACTIVE");
                                            status = RaffleStatus.Active;
                                        }

                                        pageResponse.Flush();
                                        pageResponse.SuppressContent = true;
                                        con.Close();
                                        return status;
                                    }
                                }
                            }
                            else
                            {
                                pageResponse.Write("INACTIVE");
                                pageResponse.Flush();
                                pageResponse.SuppressContent = true;
                                con.Close();
                                return RaffleStatus.InActive;
                            }
                        }



                    }
                }
                if (!objectRegistered)
                {
                    pageResponse.Write("UNREGISTERED");
                    pageResponse.Flush();
                    pageResponse.SuppressContent = true;
                    con.Close();
                    return RaffleStatus.Unregistered;
                }
            }


            return RaffleStatus.InActive;
        }

        private Boolean startNewDraw()
        {
            string maxNumberQuery = "select max(LotDrawNumber) from LotDrawHistory where ObjectGuid = '" + objectGuid + "'";
            int drawNumber = 0;

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = maxNumberQuery;     // Set CommandText to our query that will create the table

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader[0] != System.DBNull.Value && reader[0].ToString() != string.Empty)
                            {
                                drawNumber = Int32.Parse(reader[0].ToString());
                            }
                        }
                    }
                    drawNumber++;

                    string addNewRaffle = "insert into LotDrawHistory(ObjectGuid, LotDrawNumber, Duration, LotStartDateTime)values('" + objectGuid + "', " + drawNumber + ", "
                        + durationInMinutes + ", '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "')";
                    com.CommandText = addNewRaffle;
                    com.ExecuteNonQuery();

                    string updateActiveRaffle = "update LotObjectsInWorld set LotDrawNumber = " + drawNumber + " where ObjectGuid = '" + objectGuid + "'";
                    com.CommandText = updateActiveRaffle;
                    com.ExecuteNonQuery();

                    con.Close();
                }
            }

            pageResponse.Write("NEWRAFFLESTARTED");
            pageResponse.Flush();
            pageResponse.SuppressContent = true;

            return true;
        }

        private Boolean addContestant()
        {
            string sqlRaffleActive = "select * from LotObjectsInWorld where ObjectGuid = '" + objectGuid + "' and (LotDrawNumber <> null or LotDrawNumber <> 0)";

            if (this.check(true))
            {
                pageResponse.Write("ALREADYEXISTS");
                pageResponse.Flush();
                pageResponse.SuppressContent = true;
            }

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = sqlRaffleActive;     // Set CommandText to our query that will create the table

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string sqlAddContestant = "insert into LotContestents(ObjectGuid,LotDrawNumber,Name) values('" + objectGuid + "','" + reader["LotDrawNumber"].ToString() + "','" + name + "')";
                            reader.Close();
                            com.CommandText = sqlAddContestant;
                            com.ExecuteNonQuery();
                            pageResponse.Write("ADDED");
                            pageResponse.Flush();
                            pageResponse.SuppressContent = true;
                        }
                        else
                        {
                            pageResponse.Write("RAFFLEINACTIVE");
                            pageResponse.Flush();
                            pageResponse.SuppressContent = true;
                        }
                    }

                    con.Close();
                }
            }
            return true;
        }

        private Boolean check(Boolean dontEmitResponse = false)
        {
            Boolean retValue = false;
            string queryGetActiveRaffle = "select * from LotObjectsInWorld where ObjectGuid = '" + objectGuid + "'";

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = queryGetActiveRaffle;     // Set CommandText to our query that will create the table
                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string sqlReadContestant = "select * from LotContestents where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + reader["LotDrawNumber"].ToString() + "' and Name = '" + name + "'";
                            reader.Close();
                            com.CommandText = sqlReadContestant;
                            using (System.Data.SQLite.SQLiteDataReader reader1 = com.ExecuteReader())
                            {
                                if (reader1.Read())
                                {
                                    retValue = true;
                                    if (!dontEmitResponse)
                                    {
                                        pageResponse.Write("ALREADYEXISTS");
                                        pageResponse.Flush();
                                        pageResponse.SuppressContent = true;
                                    }
                                }
                                else
                                {
                                    retValue = false;
                                    if (!dontEmitResponse)
                                    {
                                        pageResponse.Write("DOESNOTEXISTS");
                                        pageResponse.Flush();
                                        pageResponse.SuppressContent = true;
                                    }
                                }
                            }
                        }
                    }
                }

                con.Close();
            }
            return retValue;
        }

        private void draw()
        {
            //string drawQuery = "select top 1 name from LotContestents where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + rdr["ActiveDrawNumber"].ToString() + "' ORDER BY NEWID()";
            string lotNumberSelectQuery = "select * from LotObjectsInWorld where ObjectGuid = '" + objectGuid + "'";
            string winnerUpdateSql = "";
            string lotNumber = "";
            string winner = "";

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = lotNumberSelectQuery;     // Set CommandText to our query that will create the table

                    using (System.Data.SQLite.SQLiteDataReader reader = com.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string drawQuery = "select name from LotContestents where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + reader["LotDrawNumber"].ToString() + "' ORDER BY RANDOM() LIMIT 1";
                            lotNumber = reader["LotDrawNumber"].ToString();
                            reader.Close();
                            com.CommandText = drawQuery;
                            using (System.Data.SQLite.SQLiteDataReader reader1 = com.ExecuteReader())
                            {
                                if (reader1.Read())
                                {
                                    winnerUpdateSql = "update LotDrawHistory set Winner = '" + reader1[0].ToString() + "' where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + lotNumber + "'";
                                    winner = reader1[0].ToString();
                                    reader1.Close();
                                    com.CommandText = winnerUpdateSql;
                                    com.ExecuteNonQuery();
                                    pageResponse.Write(winner.Replace("$", " "));
                                    pageResponse.Flush();
                                    pageResponse.SuppressContent = true;
                                }
                                else
                                {
                                    winnerUpdateSql = "update LotDrawHistory set Winner = '$$$' where ObjectGuid = '" + objectGuid + "' and LotDrawNumber = '" + lotNumber + "'";
                                    reader1.Close();
                                    com.CommandText = winnerUpdateSql;
                                    com.ExecuteNonQuery();
                                    pageResponse.Write("$$$"); // code for no winners
                                    pageResponse.Flush();
                                    pageResponse.SuppressContent = true;
                                }

                            }
                        }
                    }

                    con.Close();
                }
            }
            this.closeRaffle(true);
        }

        private void closeRaffle(Boolean dontEmitMessages = false)
        {
            string lotNumberSelectQuery = "update LotObjectsInWorld set LotDrawNumber = 0 where ObjectGuid = '" + objectGuid + "'";

            using (System.Data.SQLite.SQLiteConnection con = new System.Data.SQLite.SQLiteConnection("data source=" + HttpContext.Current.Server.MapPath("~/App_Data/Raffle.db")))
            {
                using (System.Data.SQLite.SQLiteCommand com = new System.Data.SQLite.SQLiteCommand(con))
                {
                    con.Open();                             // Open the connection to the database

                    com.CommandText = lotNumberSelectQuery;     // Set CommandText to our query that will create the table

                    com.ExecuteNonQuery();

                    if (!dontEmitMessages)
                    {
                        pageResponse.Write("CLOSED");
                        pageResponse.Flush();
                        pageResponse.SuppressContent = true;
                    }

                    con.Close();
                }
            }
        }
    }
}