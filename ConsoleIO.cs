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
				// Do your line processing logic here, on the server thread.
				Console.WriteLine("You typed: {0}", line);

				if (line == "exit")
				{
					// Console.WriteLine("OK, shutting down!");
					Console.WriteLine("This command is disabled");
					// server.Shutdown();
				}

				if (line == "ads")
				{
					var adFile = Path.Combine(Platform.SupportDir, "mrads.txt");
					if (!File.Exists(adFile))
					{
						File.WriteAllText(adFile, "[MR] Welcome to our server. Good luck, have fun!");
					}

					if (File.Exists(adFile))
					{
					var xlines = File.ReadAllLines(adFile);
					var lineStr = xlines.Random(server.Random);
					server.SendMessage(lineStr);
					}

				}

				if (line == "testmsg")
				{
					var testmsg = string.Format("Hello this is a message from the console!");
					Console.WriteLine("Sending Test Message to game");
					server.SendMessage(testmsg);
				}

				if (line == "help")
				{
					Console.WriteLine("Available commands: say, psay, status, kick, tban, admin, deladmin, ads, help. Type help commandName for more information.");
				}

				if (line.StartsWith("say "))
				{
					var smsg = line.Remove(0, 4);
					string.Format(smsg);
					Console.WriteLine(smsg);
					server.SendMessage("[MR] {0}".F(smsg));
				}

				if (line == "help psay")
				{
					Console.WriteLine("Syntax: psay userID 'message here'");
					Console.WriteLine("WARNING: Private messages are unencrypted, and may be saved to replay files. Use with caution");
				}

				if (line.StartsWith("psay ") && line.Length > 7)
				{
					var smsg = line.Remove(0, 5);
					int index = smsg.IndexOf('\'');
					if (index >= 0) {
						// Console.WriteLine("Index was greater than 0");
					int msgUID = Int32.Parse(smsg.Substring(0, index));
					var msgConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == msgUID);
					if (msgConn != null)
					{
						// Console.WriteLine("msgConn was not null");
						smsg = smsg.Remove(0, index - 1);
						Match match = Regex.Match(smsg, @"'([^']*)");
						if (match.Success)
						{
							var msgClient = server.GetClient(msgConn);
							server.SendOrderTo(msgConn, "Message", "[MR] Private Msg: {0}".F(match.Groups[1].Value));
							Console.WriteLine("[MR] PMsg to {0}: {1}", msgClient.Name, match.Groups[1].Value);
						}
					}
				}
									}

				if (line.StartsWith("map ") && server.State != ServerState.GameStarted)
				{
					// lock(server.LobbyInfo)
					// {
					var mapID = line.Remove(0, 4);
					string.Format(mapID);
					// server.Map = server.ModData.MapCache[mapID];;
					// LobbyCommands.LoadMapSettings(server, server.LobbyInfo.GlobalSettings, server.Map.Rules);
					Console.WriteLine("Changed map to {0}", mapID);
					server.SendMessage("[MR] Changed map to {0}".F(mapID));
					// }
				}

				if (line == "help kick")
				{
					Console.WriteLine("Syntax: kick UserID");
				}

				if (line.StartsWith("kick "))
				{
					var aUID = line.Remove(0, 5);
					int kickUID;
					int.TryParse(aUID, out kickUID);
					var kickConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == kickUID);
					if (kickConn != null)
					{
						var kickClient = server.GetClient(kickConn);
						Console.WriteLine("Kicking CID {0} - {1}", kickUID.ToString(), kickClient.Name);
						server.SendMessage("[MR] Kicking CID {0} - {1}".F(kickUID.ToString(), kickClient.Name));
						server.SendOrderTo(kickConn, "ServerError", "You were kicked by the console.");
						server.DropClient(kickConn);
						if (server.State != ServerState.GameStarted)
						{
							server.SyncLobbyClients();
							server.SyncLobbySlots();
						}
					}
				}

				if (line.StartsWith("tban "))
				{
					var aUID = line.Remove(0, 5);
					int banUID;
					int.TryParse(aUID, out banUID);
					var banConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == banUID);
					if (banConn != null)
					{
						var banClient = server.GetClient(banConn);
						server.SendOrderTo(banConn, "ServerError", "You were temp banned by the console.");
						server.TempBans.Add(banClient.IPAddress);
						server.DropClient(banConn);
						Console.WriteLine("Tempbanning CID {0} - {1}", banUID.ToString(), banClient.Name);
						server.SendMessage("[MR] Tempbanning CID {0} - {1}".F(banUID.ToString(),banClient.Name));
						if (server.State != ServerState.GameStarted)
						{
							server.SyncLobbyClients();
							server.SyncLobbySlots();
						}
					}
				}

				if (line.StartsWith("ipban "))
				{
					string aIPstr = line.Remove(0, 5);
					Regex validIpV4AddressRegex = new Regex(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.IgnoreCase);
					if (!string.IsNullOrWhiteSpace(aIPstr))
					{
						bool flag = validIpV4AddressRegex.IsMatch(aIPstr.Trim());
						if (flag)
						{
							server.TempBans.Add(aIPstr.Trim());
							Console.WriteLine("TempIP Ban {0}", aIPstr.Trim());
							Log.Write("server", "TempIP Ban {0}", aIPstr.Trim());
						}
						else
						{
							Console.WriteLine("{0} is not a valid IP", aIPstr.ToString());
						}

					}
				}

				if (line == "banlist")
				{
					foreach (var item in server.TempBans)
					Console.WriteLine(item);
				}

				if (line.StartsWith("admin "))
				{
					var aUID = line.Remove(0, 6);
					int adminUID;
					int.TryParse(aUID, out adminUID);
					var adminConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == adminUID);
					if (adminConn != null)
					{
						var adminClient = server.GetClient(adminConn);
						adminClient.IsAdmin = true;
						Console.WriteLine("Setting Admin {0} - {1}]", adminUID.ToString(), adminClient.Name);
						server.SendMessage("[MR] Setting Admin {0} - {1}".F(adminUID.ToString(), adminClient.Name));
						server.SendOrderTo(adminConn, "Message", "You have been set as an admin by the console");
						if (server.State != ServerState.GameStarted)
						{
							server.SyncLobbyClients();
							server.SyncLobbySlots();
						}
					}
				}

				if (line.StartsWith("deladmin "))
				{
					var aUID = line.Remove(0, 9);
					int adminUID;
					int.TryParse(aUID, out adminUID);
					var adminConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == adminUID);
					if (adminConn != null)
					{
						var adminClient = server.GetClient(adminConn);
						adminClient.IsAdmin = false;
						Console.WriteLine("Removing Admin {0} - {1}]", adminUID.ToString(), adminClient.Name);
						server.SendMessage("[MR] Removing Admin {0} - {1}".F(adminUID.ToString(), adminClient.Name));
						server.SendOrderTo(adminConn, "Message", "Your admin status was revoked by the console");
						if (server.State != ServerState.GameStarted)
						{
						server.SyncLobbyClients();
						server.SyncLobbySlots();
						}
					}
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
			Console.WriteLine("Console input thread started");
			Console.WriteLine("MR-bridge Pre-release v 0.00");
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
