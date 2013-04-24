using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBC.Database;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json;
namespace GeoCode
{
    public class BingMaps
    {
        public string authenticationResultCode { get; set; }
        public ResourceSets[] resourceSets { get; set; }
    }

    public class ResourceSets
    {
        public int estimatedTotal { get; set; }
        public Resources[] resources { get; set; }
    }

    public class Resources
    {
        public string name { get; set; }
        public Point point { get; set; }
    }

    public class Point
    {
        public string type { get; set; }
        public string[] coordinates;
    }

    public class Coordinates
    {
        //public float coordinates; 
    }


    class Program
    {
        static void Main(string[] args)
        {
            // Creates two database connections
            db db = new db("DB");
            db dbUpdate = new db("DB");

            // Gets the default Parameters so I can just update instead.
            dbUpdate.ParameterAdd("@Latitude", "");
            dbUpdate.ParameterAdd("@Longitude", "");
            dbUpdate.ParameterAdd("@ZipCode", "");

            // Queries the DB
            var sqlStr = "SELECT ZipCode FROM ZipCodes WHERE ZipCode > 48101";
            var ex = db.ExecuteSqlReader(sqlStr);
            while (db.Reader.Read())
            {
                Console.WriteLine("Updating " + db.Reader[0]);
                // Makes the call to bing. Only could get US to work for now as a zip code was giving me heck.
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://dev.virtualearth.net/REST/v1/Locations/US/-/" + db.Reader[0] + "/?key=" + System.Configuration.ConfigurationManager.AppSettings["BingKey"]);
                var resp = req.GetResponse();
                var stream = resp.GetResponseStream();
                var strReader = new StreamReader(stream);
                var str = (strReader.ReadToEnd());
                var data = JsonConvert.DeserializeObject<BingMaps>(str);

                // Update the datbase
                sqlStr = "UPDATE ZipCodes SET Latitude=@Latitude, Longitude=@Longitude WHERE ZipCode=@ZipCode";
                dbUpdate.ParameterEdit("@Latitude", data.resourceSets[0].resources[0].point.coordinates[0]);
                dbUpdate.ParameterEdit("@Longitude", data.resourceSets[0].resources[0].point.coordinates[1]);
                dbUpdate.ParameterEdit("@ZipCode", db.Reader[0]);
                ex = dbUpdate.ExecuteSql(sqlStr);
                if (ex != null)
                {
                    Console.WriteLine("Zip Code: " + db.Reader[0] + " had an error in updating. Error -->" + ex.Message);
                }
                Thread.Sleep(1000);
            }
            dbUpdate.CloseConnection();
            db.CloseConnection();
        }
    }
}
