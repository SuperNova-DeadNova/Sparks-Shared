/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Net;
using GoldenSparks.Network;
using GoldenSparks.Tasks;

namespace GoldenSparks 
{
    /// <summary> Checks for and applies software updates. </summary>
    public static class Updater 
    {    
        public static string SourceURL = "https://github.com/GoldenSparks/Sparks/";
        public const string BaseURL    = "https://github.com/GoldenSparks/Sparks/blob/Sparkie/";
        public const string UploadsURL = "https://github.com/GoldenSparks/Sparks/tree/Sparkie/Uploads";
                public const string UpdatesURL = "https://github.com/GoldenSparks/Sparks/raw/Sparkie/Uploads/";
        const string CurrentVersionURL = BaseURL + "Uploads/current_version.txt";
        const string dllURL = UpdatesURL + "Sparkie_.dll";
        const string guiURL = UpdatesURL + "Sparkie.exe";
        const string cliURL = UpdatesURL + "SparkieCLI.exe";

        public static event EventHandler NewerVersionDetected;
        
        public static void UpdaterTask(SchedulerTask task) {
            UpdateCheck();
            task.Delay = TimeSpan.FromHours(2);
        }

        static void UpdateCheck() {
            if (!Server.Config.CheckForUpdates) return;

            try {
                if (!NeedsUpdating()) {
                    Logger.Log(LogType.SystemActivity, "No update found!");
                } else if (NewerVersionDetected != null) {
                    NewerVersionDetected(null, EventArgs.Empty);
                }
            } catch (Exception ex) {
                Logger.LogError("Error checking for updates", ex);
            }
        }
        
        public static bool NeedsUpdating() {
            using (WebClient client = HttpUtil.CreateWebClient()) {
                string latest = client.DownloadString(CurrentVersionURL);
                return new Version(latest) > new Version(Server.Version);
            }
        }

        public static void PerformUpdate() {
            try {
                try {
                    DeleteFiles("Sparkie_.update", "Sparkie.update", "SparkieCLI.update",
                                "prev_Sparkie_.dll", "prev_Sparkie.exe", "prev_SparkieCLI.exe");
                } catch {
                }
                
                WebClient client = HttpUtil.CreateWebClient();
                client.DownloadFile(dllURL, "Sparkie_.update");
                client.DownloadFile(guiURL, "Sparkie.update");
                client.DownloadFile(cliURL, "SparkieCLI.update");

                Server.SaveAllLevels();
                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players) pl.SaveStats();
                
                string serverDLL = Server.GetServerDLLPath();
                
                // Move current files to previous files (by moving instead of copying, 
                //  can overwrite original the files without breaking the server)
                AtomicIO.TryMove(serverDLL,         "prev_MSparkie_.dll");
                AtomicIO.TryMove("Sparkie.exe",    "prev_Sparkie.exe");
                AtomicIO.TryMove("SparkieCLI.exe", "prev_SparkieCLI.exe");

                // Move update files to current files
                AtomicIO.TryMove("Sparkie_.update",   serverDLL);
                AtomicIO.TryMove("Sparkie.update",    "Sparkie.exe");
                AtomicIO.TryMove("SparkieCLI.update", "SparkieCLI.exe");                             

                Server.Stop(true, "Updating server.");
            } catch (Exception ex) {
                Logger.LogError("Error performing update", ex);
            }
        }
        
        static void DeleteFiles(params string[] paths) {
            foreach (string path in paths) { AtomicIO.TryDelete(path); }
        }
    }
}
