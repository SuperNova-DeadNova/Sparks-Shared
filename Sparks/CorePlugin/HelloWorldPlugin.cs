using System;
using GoldenSparks.Tasks;

namespace GoldenSparks
{
    public class HelloWorld : Plugin
    {
        public override string name { get { return "Saying hello"; } } // to unload, /punload hello
        public override string creator { get { return Server.SoftwareName + " team"; } }
        public override string GoldenSparks_Version { get { return Server.Version; } }
        public override void Load(bool startup)
        {
            Server.Background.QueueOnce(SayHello, null, TimeSpan.FromSeconds(10));
        }

        void SayHello(SchedulerTask task)
        {

            Command.Find("say").Use(Player.Sparks, Server.SoftwareName + " " + Server.InternalVersion + " online! ^w^");

            Logger.Log(LogType.SystemActivity, "&fHello World!");
        }
        public override void Unload(bool shutdown)
        {
        }

        public override void Help(Player p)
        {
            p.Message("");
        }
    }
}