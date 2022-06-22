using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nyo.Fr.EmuNWA
{
    class CommandHandler
	{
		static public BizHawk.Client.Common.ApiContainer APIs = null;
		static public bool romloadedOnce = false;
		static public Dictionary<NWACommand, Func<NWAClient, String[], bool>> commandsMap = new Dictionary<NWACommand, Func<NWAClient, String[], bool>>
		{
			{ NWACommand.EMULATOR_INFO, CommandHandler.emulatorInfo },
			{ NWACommand.EMULATION_STATUS, CommandHandler.emulationStatus },
			{ NWACommand.CORES_LIST, CommandHandler.coresList },
			{ NWACommand.CORE_INFO, CommandHandler.coreInfo },
			{ NWACommand.MY_NAME_IS, CommandHandler.myNameIs},
			{ NWACommand.CORE_CURRENT_INFO, CommandHandler.currentCoreInfo},
			{ NWACommand.CORE_MEMORIES, CommandHandler.coreMemories },
			{ NWACommand.CORE_READ, CommandHandler.coreRead },
			{ NWACommand.bCORE_WRITE, CommandHandler.coreWrite }
		};
		static public Dictionary<NWACommand, Func<NWAClient, bool>> binaryCommandHandlerMap = new Dictionary<NWACommand, Func<NWAClient, bool>>()
		{
			{ NWACommand.bCORE_WRITE, CommandHandler.coreWriteHandler }
		};


        static bool emulatorInfo(NWAClient client, String[] args)
		{
			Dictionary<String, String> reply = new Dictionary<string, string>();
			reply["name"] = "BizHawk";
			reply["version"] = BizHawk.Common.VersionInfo.GetEmuVersion();
			reply["id"] = "Happy Skarsnik";
			reply["nwa_version"] = "1.0";
			foreach(var plop in commandsMap)
            {
				if (reply.ContainsKey("commands"))
					reply["commands"] += "," + plop.Key.ToString();
				else
					reply["commands"] = plop.Key.ToString();
            }
			client.sendHashReply(reply);
			return true;
		}
		static bool emulationStatus(NWAClient client, String[] args)
		{
			Dictionary<String, String> reply = new Dictionary<string, string>();
			if (APIs == null)
			{
				reply["state"] = "no_game";
			}
			else
			{
				if (APIs.GameInfo.GetGameInfo().System == VSystemID.Raw.NULL)
				{
					reply["state"] = "no_game";
				}
				else
				{
					if (APIs.EmuClient.IsPaused())
						reply["state"] = "paused";
					else
						reply["state"] = "running";
					reply["game"] = APIs.GameInfo.GetGameInfo().Name;
				}
			}
			client.sendHashReply(reply);
			return true;
		}
		static bool myNameIs(NWAClient client, String[] args)
        {
			if (args == null || args.Length != 1)
            {
				client.sendError(ErrorKind.command_error, "MY_NAME_IS accept one argument <name>");
				return true;
            }
			client.server.newClientNameCallBack?.Invoke(client.name, args[0]);
			client.name = args[0];
			Dictionary<String, String> reply = new Dictionary<String, String>();
			reply["name"] = client.name;
			
			client.sendHashReply(reply);
			return true;
        }
		static bool coresList(NWAClient client, String[] args)
        {
			String filterPlateform = "";
			if (args != null && args.Length == 1)
				filterPlateform = args[0];
			List<Dictionary<String, String>> reply = new List<Dictionary<string, string>>();
			foreach (var coresInfo in CoreInventory.Instance.AllCores)
            {
				if (filterPlateform != "" && coresInfo.Key != filterPlateform)
					continue;
				foreach (var cores in coresInfo.Value)
                {
					Dictionary<String, String> coreInfo = new Dictionary<String, String>();
					coreInfo["name"] = cores.Name;
					coreInfo["platform"] = coresInfo.Key;
					reply.Add(coreInfo);
				}
            }
			client.sendHashReply(reply);
			return true;
        }
		static bool coreInfo(NWAClient client, String[] args)
        {
			if (args == null || args.Length > 1)
            {
				client.sendError(ErrorKind.command_error, "Core info only take one argument <core_name>");
				return true;
            }
			Dictionary<String, String> reply = new Dictionary<string, string>();
			foreach (var coresInfo in CoreInventory.Instance.AllCores)
            {
				foreach (var core in coresInfo.Value)
				{
					if (core.Name == args[0])
					{
						reply["name"] = core.Name;
						reply["platform"] = coresInfo.Key;
						reply["author"] = core.CoreAttr.Author;
					}
				}
			}
			if (reply.Count > 0)
				client.sendHashReply(reply);
			else
				client.sendError(ErrorKind.invalid_argument, "There is no core named " + args[0]);
			return true;
		}
		static bool currentCoreInfo(NWAClient client, String[] args)
        {
			String current = APIs.Emulation.GetSystemId();
			if (current == null)
            {
				client.sendError(ErrorKind.command_error, "No core loaded");
				return true;
            }
			String[] p = new string[1];
			p[0] = current;
			return coreInfo(client, p);
        }

		static bool coreMemories(NWAClient client, String[] args)
        {
			if (APIs == null || APIs.GameInfo.GetGameInfo().System == VSystemID.Raw.NULL)
            {
				client.sendError(ErrorKind.command_error, "No core loaded");
				return true;
            }
			List<Dictionary<String, String>> reply = new List<Dictionary<String, String>>();
			foreach (var domainName in APIs.Memory.GetMemoryDomainList())
            {
				Dictionary<String, String> domainRep = new Dictionary<String, String>();
				domainRep["name"] = Common.domainToNWAName(domainName, APIs.GameInfo.GetGameInfo().System);
				domainRep["access"] = "rw";
				domainRep["size"] = APIs.Memory.GetMemoryDomainSize(domainName).ToString();
				reply.Add(domainRep);
			}
			client.sendHashReply(reply);
			return true;
        }

		static long coreReadWrite(NWAClient client, String[] args, bool write)
		{
			long totalSize = 0; // This is only used for core_write
								// 0 mean you don't have a size
								// -1 mean an error
			if (args == null || args.Length == 0)
			{
				client.sendError(ErrorKind.invalid_argument, "You need to at least specify a domain to read for CORE_READ or CORE_WRITE");
				return -1;
			}
			if (args.Length >= 1)
			{
				
				String domain = Common.NWANameToDomain(args[0], APIs.GameInfo.GetGameInfo().System);
				if (!APIs.Memory.GetMemoryDomainList().Contains(domain))
				{
					client.sendError(ErrorKind.command_error, "The specified domain does not exists");
					return -1;
				}
				// Whole domain
				if (args.Length == 1)
				{
					totalSize = APIs.Memory.GetMemoryDomainSize(domain);
					client.addMemoryAccess(domain, 0, APIs.Memory.GetMemoryDomainSize(domain), write);
				}
				else
				{
					if (args.Length > 3)
					{
						if (args.Length % 2 == 0)
						{
							client.sendError(ErrorKind.invalid_argument, "You are not allowed to ommit the size when doing multiple read");
							return -1;
						}
					}
					long offset = 0;
					uint size = 0;
					for (int i = 1; i < args.Length; i++)
					{
						if (i % 2 == 0)
						{
							size = (uint)Common.NWANumber(args[i]);
							client.addMemoryAccess(domain, (uint)offset, size, write);
							totalSize += size;
							offset = -1;
						}
						else
						{
							offset = (uint)Common.NWANumber(args[1]);
						}
					}
					if (offset != -1)
					{
						client.addMemoryAccess(domain, (uint)offset, APIs.Memory.GetMemoryDomainSize(domain) - (uint)offset, write);
					}
				}
				client.server.addMemoryAccessClient(client);
			}
			return totalSize;
		}
		static bool coreRead(NWAClient client, String[] args)
        {
			coreReadWrite(client, args, false);
			return true;
        }

		static bool coreWrite(NWAClient client, string[] args)
		{
			client.expectedBinaryDataSize = coreReadWrite(client, args, true);
			client.readyToWrite = false;
			return true;
		}

		static bool coreWriteHandler(NWAClient client)
        {
			client.readyToWrite = true;
			return true;
        }

		static public void actualReadMemory(NWAClient client)
        {
			List<byte> bytes = new List<byte>();
			foreach (var mem in client.memoryAccess)
			{
				Console.WriteLine("actual read: Domain " + client.memoryDomain + " O:" + mem.offset + "Size :" + mem.size);
				var readed = APIs.Memory.ReadByteRange(mem.offset, Convert.ToInt32(mem.size), client.memoryDomain);
				Console.WriteLine("Readed : " + readed.Count);
				bytes.AddRange(readed);
			}
			Console.WriteLine("Passing : " + bytes.Count);
			client.sendData(bytes);
			client.memoryAccess.Clear();
		}

        static public void actualWriteMemory(NWAClient client)
        {
			// We need to fix the size of the write
			Console.WriteLine("Write Memory");
			Console.WriteLine("Client is null ? :" + client == null);
			Console.WriteLine(client.binaryBuffer.ToString());
			if (client.expectedBinaryDataSize == 0)
            {
				client.memoryAccess[0].size = (uint)client.binaryBuffer.Length;
            }
			int start = 0;
			List<byte> bytesToWrite = client.binaryBuffer.ToList();
            foreach (var mem in client.memoryAccess)
            {
				Console.WriteLine("Trying to write " + mem.size);
				APIs.Memory.WriteByteRange(mem.offset, bytesToWrite.GetRange(start, (int)mem.size), client.memoryDomain);
				start += (int)mem.size;
            }
			client.sendOk();
			client.memoryAccess.Clear();
        }
    }
}
