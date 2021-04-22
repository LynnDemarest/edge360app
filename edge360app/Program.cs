using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace edge360app
{
    class Program
    {
        const string dashes = "--------------------------------------------------------------";
        const bool REMOVE_DUPLICATES = true;  // duplicates will be removed. 
        const bool ORDER_MATTERS = false;     // non-ordered sets of camera models. If the models are in a different order, they'll still be considered equal. 
        const bool CASE_MATTERS = true;       // camera models that differ only in case will be seen as different 

        const int NUMBER_OF_LEADERS = 10;
        const string ALL_COMBINATIONS = @"ALL UNIQUE COMBINATIONS
Combinations with the same number of occurrences are sorted alphabetically.
Camera models sorted alphabetically.";

        const string PATH = @"C:\Coding\edge360\edge360app\edge360app";
        const string FILENAME = "sites.json";


        // Usage: See createOutput.bat in bin/Debug/netcoreapp3.1, 
        //        which pipes output to bin/Debug/output.txt
        //
        //
        static void Main(string[] args)
        {
            ProcessFile(Path.Combine(PATH, FILENAME));

            // If interactive, pause output screen,
            // otherwise, just exit. 
            if (args.Count() > 0)
            {
                if (args[0] == "-i") Console.ReadLine();
            }
        }
        private static void ProcessFile(string path)
        {
            try
            {
                // See CameraSet class defined below this class.
                // It's essentially an array of camera models loaded from the "cameras" element in the json file. 
                // CameraSet also overrides ToString to output a comma-separated list of trimmed camera models. 
                CameraSet[] cameraSets = System.Text.Json.JsonSerializer.Deserialize<CameraSet[]>(File.ReadAllText(path));

                // key   : camera model list string
                // value : # of occurrences of this (unordered) combination
                Dictionary<string, int> combinationCounts = GetCountDictionary(cameraSets);

                DisplayLeaders(combinationCounts, NUMBER_OF_LEADERS);

                DisplayAllCombinations(combinationCounts);

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }


        // GetCountDictionary
        // Returns a dictionary with the camera model list string as the key and the number of occurrences found as the value.
        // Note that cameraSet.Clean optionally orders the camera models within this string.
        private static Dictionary<string, int> GetCountDictionary(CameraSet[] cameraSets)
        {
            Dictionary<string, int> combinationCounts = new Dictionary<string, int>();
            foreach (var cameraSet in cameraSets)
            {
                // If order matters, no sorting will be done. Otherwise the models will be sorted. 
                // If case matters, the models will not be set to lowercase, otherwise they will. 
                cameraSet.Clean(bRemDupes: REMOVE_DUPLICATES, bOrderMatters: ORDER_MATTERS, bCaseMatters: CASE_MATTERS);

                string temp = cameraSet.ToString();  // Ex: "ACTi ACM-3511, ACTi ACM-8221, Axis Q1755"
                if (combinationCounts.Keys.Contains(temp))
                {
                    combinationCounts[temp]++;
                }
                else
                {
                    combinationCounts.Add(temp, 1);
                }
            }
            return combinationCounts;
        }

        // DisplayLeaders
        // Displays the camera model combinations with the most occurrences. 
        // Stops when it gets to 'limit' number of model list string.
        //
        private static void DisplayLeaders(Dictionary<string, int> combinationCounts, int limit)
        {
            var sortedItems = from pair in combinationCounts
                              orderby pair.Value descending
                              select pair;

            Console.WriteLine($"Top {limit}");
            Console.WriteLine(dashes);
            int x = 0;
            foreach (KeyValuePair<string, int> pair in sortedItems)
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
                x++;
                if (x == limit) break;
            }

        }


        // DisplayAllCombinations
        // Sorted by descending count. 
        // Camera model lists are sorted within a single list, then the lists themselves are sorted within a single count category. 
        private static void DisplayAllCombinations(Dictionary<string, int> combinationCounts)
        {
            // Create a dictionary with the count as key and a sorted List of strings as the items with that count. 
            Dictionary<int, List<string>> byCount = new Dictionary<int, List<string>>();
            foreach (string modelList in combinationCounts.Keys)
            {
                int cnt = combinationCounts[modelList];
                if (byCount.Keys.Contains(cnt))
                    byCount[cnt].Add(modelList);  // add the model list string to the list of strings related to the given count cnt.
                else
                    byCount.Add(cnt, new List<string> { modelList }); // first one, so init the List 
            }
                        
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(ALL_COMBINATIONS);
            Console.WriteLine(dashes);
            foreach (var countList in byCount.OrderByDescending(x => x.Key))
            {
                // 'key,' an int in the byCount dictionary, is cast to a string here
                // so we can replace it with "  " below. We only output counts once so camera model lists with the same 
                // number of occurrences are easily seen. 
                string key = countList.Key.ToString();  

                countList.Value.Sort();   // Sort the List of camera model list strings. The Value is List<string>
                foreach (string p in countList.Value)
                {
                    Console.WriteLine($"{key} = {p}");
                    key = "  ";      // so repeated counts will not be redisplayed
                }
            }
        }
    }

    // CameraSet
    // Includes one property to hold a string array of camera models. 
    // Clean method massages the data as directed.
    // ToString is overridden to return a comma-delimited list of the models in 'cameras'.
    public class CameraSet
    {
        public string[] cameras { get; set; }   // Note: "cameras" matches the element in the json file 

        // Clean
        // Cleans the cameras array, removing dupes, sorting and re-casing depending on arguments.
        //
        public void Clean(bool bRemDupes, bool bOrderMatters, bool bCaseMatters)
        {
            if (!bOrderMatters) Array.Sort(cameras);

            if (!bCaseMatters) cameras = cameras.Select(x => x.ToLower()).ToArray();

            if (bRemDupes)
            {
                string[] dcameras = cameras.Distinct().ToArray();

                // I just put this here to see if there were dupes. There were! 
                if (dcameras.Count() != cameras.Count() && false)
                {
                    Console.WriteLine("---------------------------------------------");
                    Console.WriteLine("Duplicate found and removed.");
                    //Console.WriteLine(String.Join(dcameras, ","));
                    //Console.WriteLine(cameras.ToString());
                    Console.WriteLine(string.Join(", ", dcameras));
                    Console.WriteLine(string.Join(", ", cameras));
                    Console.WriteLine("---------------------------------------------");

                    //Console.ReadLine();
                }

                cameras = dcameras;
            }
        }
        // We're interested in unique combinations, so we order the elements
        // in case one record has the same elements in a different order. 
        // Order does not matter.
        //
        //
        override public string ToString()
        {
            string s = "";
            string sComma = "";
            foreach (var c in cameras)
            {
                s += sComma + c.Trim();  // also trim the data for comparison 
                sComma = ", ";
            }
            return s;
        }
    }
}
