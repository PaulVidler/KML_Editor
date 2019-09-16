using SharpKml.Base;
using SharpKml.Dom;
using SharpKml.Engine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System;

namespace KML_Editor
{
    class Program
    {

        static void Main(string[] args)
        {

            Console.WriteLine("-- Kml converter -- \nEnter the complete file path without quotes: ");
            string filePath = Console.ReadLine();

            FPConverter(filePath);

            Console.WriteLine("Process complete - Press enter to exit");
            Console.ReadKey();

        }


        private static void FPConverter(string file)
        {
            var lpFile = file;

            if (!File.Exists(lpFile))
            {
                return;
            }

            XNamespace ns = "http://earth.google.com/kml/2.2";
            var doc = XDocument.Load(lpFile);

            var placemarks = doc.Root
                           .Element(ns + "Document")
                           .Elements(ns + "Folder")
                           .Elements(ns + "Placemark");

            var kml = new Kml();

            var document = new Document();

            document.Name = Path.GetFileNameWithoutExtension(lpFile);


            foreach (var obj in placemarks)
            {
                var name = obj
                    .Element(ns + "name");

                
                Folder folder = new Folder()
                {
                    Name = name.Value,
                };

                Placemark newLine = new Placemark();
                newLine.Name = name.Value;
                
                var coordinates = obj
                    .Elements(ns + "LineString")
                    .Elements(ns + "coordinates");


                var runNumber = name.Value.Trim();
                var runsubs = runNumber.Substring(3);

                const double Scale = 0.3048;


                if (!string.IsNullOrEmpty(runNumber))
                {
                    if (coordinates.Any())
                    {
                        foreach (var coords in coordinates)
                        {
                            
                            var value = coords.Value;
                            var num = Regex.Match(value, @"[0-9]{0,4}\s+");

                            LineString line = new LineString();

                            var newCoords = coords.Value.Trim();

                            // splitting string values from original kml
                            // into usable data for Vector class
                            string[] splitCoords = newCoords.Split(' ');
                            string[] splitCoords1 = splitCoords[0].Split(',');
                            string[] splitCoords2 = splitCoords[1].Split(',');

                            double long1 = double.Parse(splitCoords1[0]);
                            double lat1 = double.Parse(splitCoords1[1]);
                            double height1 = double.Parse(splitCoords1[2]);

                            double long2 = double.Parse(splitCoords2[0]);
                            double lat2 = double.Parse(splitCoords2[1]);
                            double height2 = double.Parse(splitCoords2[2]);


                            CoordinateCollection CoordsCollection = new CoordinateCollection();
                            CoordsCollection.Add(new Vector(lat1, long1, height1));
                            CoordsCollection.Add(new Vector(lat2, long2, height2));

                            // Start placemark code
                            Placemark flightLabel = new Placemark();
                            
                            Point point = new Point();
                            Vector pointVector = new Vector();

                            pointVector.Latitude = ReturnCentre(lat1, lat2);
                            pointVector.Longitude = ReturnCentre(long1, long2);

                            point.Coordinate = pointVector;

                            flightLabel.Geometry = point;
                            // end placemark code

                            line.Coordinates = CoordsCollection;
                            newLine.Geometry = line;

                            //puts placemark into <folder>
                            folder.AddFeature(newLine);
                            

                            //add the folder to the document.
                            document.AddFeature(folder);

                            
                            if (num.Success)
                            {
                                var valueStr = num.Value.Trim();

                                if (int.TryParse(valueStr, out var height))
                                {
                                    checked
                                    {
                                        var feet = (height / Scale);
                                        flightLabel.Name = runNumber +"/" +  Convert.ToInt32(feet) + "ft";
                                    }
                                }
                            }

                            folder.AddFeature(flightLabel);

                            kml.Feature = document;
                            KmlFile kmlFile = KmlFile.Create(kml, true);

                            using (var fs = File.OpenWrite(document.Name + "-TEST-ONLY-DO-NOT-FLY.kml"))
                            {
                                kmlFile.Save(fs);
                            }
                        }
                    }
                }
            }
        }



        public static double ReturnCentre(double geoCoord1, double geoCoord2)
        {
            return (geoCoord1 + geoCoord2) / 2;
        }



    }
}
