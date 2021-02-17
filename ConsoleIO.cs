using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Support;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
    public class ConsoleIO : ServerTrait, ITick, INotifyServerStart, INotifyServerShutdown
    {
		Thread consoleThread;
		ConcurrentQueue<string> queue;

		public void ServerStarted(S server)
        {
            queue = new ConcurrentQueue<string>();
            consoleThread = new Thread(ORAConsole) { IsBackground = true };
            consoleThread.Start(queue);
        }

		public void Tick(S server)
        {
            if (queue.TryDequeue(out var line))
            {

                Console.WriteLine("You typed: {0}", line);

                if (line == "exit")
                {
                    Console.WriteLine("OK, shutting down!");
                    server.Shutdown();
                }

                if (line == "testmsg")
                {
                    var testmsg = string.Format("Hello this is a message from the console!");
                    Console.WriteLine("Sending Test Message to game");
                    server.SendMessage(testmsg);
                }
		
		if (line.StartsWith("say "))
                {
                    var smsg = line.Remove(0, 4);
                    string.Format(smsg);
                    Console.WriteLine(smsg);
                    server.SendMessage(smsg);
                }

                if (line == "kick0")
                {
					var aUID = 0;
					var kickUID = server.LobbyInfo.ClientWithIndex(aUID);
					Console.WriteLine("Kicking CID {0}", kickUID);
					var testmsg1 = string.Format("Kicking CID {0}", kickUID);
					LobbyCommands.Kick(kickUID);
					server.SendMessage(testmsg1);


					// server.DropClient(kickUID);
                }

                if (line == "changemap")
                {
					var mapid = "5706ef75deb3c125b4f77e834d42e99eb1ebde73";
					Order.Command("map " + mapid);
                }

                if (line == "status")
                {
					Console.WriteLine(string.Format("CurMap: {0} \n", server.Map.Title));
					Console.WriteLine(string.Format("MapHash: {0} \n", server.LobbyInfo.GlobalSettings.Map));
					Console.WriteLine(string.Format("CurConnections: {0} \n", server.LobbyInfo.Clients.Count()));
					Console.WriteLine(string.Format("MapMaxPlayers: {0} \n", server.LobbyInfo.Slots.Count()));
					Console.WriteLine(string.Format("GameState: {0} \n", server.State.ToString()));

					var testbuffer = server.Conns.ToArray();
					foreach (var item in testbuffer)
						{
                        var clientIdT = server.GetClient(item); // make the temporary array the client ID
                        Console.WriteLine(string.Format("ID: {0} IP: {1} TEAM: {2} SPEC: {3}, NAME: {4} \n", clientIdT.Index.ToString(), clientIdT.IPAddress.ToString(), clientIdT.Team.ToString(), clientIdT.IsObserver.ToString(), clientIdT.Name.ToString()));
						}
                }
            }
        }

		public void ServerShutdown(S server)
        {
            consoleThread.Abort();
        }

		public static void ORAConsole(object obj)
        {
            Console.WriteLine("Console input thread started.");
            try
			{
				var queue = (ConcurrentQueue<string>)obj;
				while (true)
                {
                    var line = Console.ReadLine(); // Get string from user
                    queue.Enqueue(line);
                }
            }
            catch (ThreadAbortException)
			{
                Console.WriteLine("Console input thread stopped.");
            }
        }
    }
}
