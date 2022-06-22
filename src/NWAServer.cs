using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nyo.Fr.EmuNWA
{
    public static class Common
    {
		public static string domainToNWAName(string cname, string system)
		{
			if (system == "SNES")
			{
				if (cname == "CARTRAM")
				{
					return "SRAM";
				}
				else
				{
					return cname;
				}
			}
			return cname;
		}
		public static string NWANameToDomain(string cname, string system)
        {
			if (system == "SNES")
			{
				if (cname == "SRAM")
				{
					return "CARTRAM";
				}
				else
				{
					return cname;
				}
			}
			return cname;
		}
		public static int NWANumber(String intString)
        {
			if (intString[0] == '$')
            {
				return int.Parse(intString.Substring(1), System.Globalization.NumberStyles.HexNumber);
            }
			return int.Parse(intString);
        }
	}
	
	enum NWACommand
	{
		NONE,
		EMULATOR_INFO,
		EMULATION_STATUS,
		CORE_CURRENT_INFO,
		CORES_LIST,
		CORE_INFO,
		MY_NAME_IS,
		CORE_MEMORIES,
		CORE_READ,
        bCORE_WRITE
    }
	enum ErrorKind
	{
		protocol_error,
		command_error,
		invalid_command,
		invalid_argument
	}
	class MemoryAccess
    {
		public uint offset = 0;
		public uint size = 0;
    }
	class NWAClient
	{
		public enum State
		{
			WaitingForCommand,
			GettingCommand,
			ExpectingBinaryData,
			GettingBinaryData
		}
		public NWAServer server = null;
		public String	name = null;
		public Socket	socket = null;
		public State	state = State.WaitingForCommand;

		public const int BufferSize = 1024;
		public const int BufferCommandSize = 4096;
		public byte[]	buffer = new byte[BufferSize];
		public byte[]	bufferCommand = new byte[BufferCommandSize];
		public int		bufferCommandPos = 0;
		public int		readed = 0;
		public byte[]	binaryData = null;
		public byte[]	binaryHeader = new byte[4];
        public int		binaryHeaderPos = 0;
        public byte[]	binaryBuffer;
		public uint		binaryBufferPos = 0;
		public long		expectedBinaryDataSize = 0;
		public NWACommand currentCommand = NWACommand.NONE;

		public bool writeAccess = false;
		public String memoryDomain = null;
		public List<MemoryAccess> memoryAccess;
        public bool shallowBinaryBlock = false;
        public bool readyToWrite = false;
		static int clientId = 0;

        public NWAClient(NWAServer Pserver)
        {
			server = Pserver;
			memoryAccess = new List<MemoryAccess>();
			name = "Client " + clientId++;
		}
		public void addMemoryAccess(String domain, uint offset, uint size, bool write)
        {
			memoryDomain = domain;
			MemoryAccess memAccess = new MemoryAccess();
			memAccess.offset = offset;
			memAccess.size = size;
			Console.WriteLine("Adding memory access : " + memAccess.offset + " : " + memAccess.size);
			memoryAccess.Add(memAccess);
			writeAccess = write;
        }
		
		public void sendError(ErrorKind kind, String reason)
		{
			byte[] errorBuffer = Encoding.ASCII.GetBytes("\nerror:" + kind.ToString() + "\nreason:");
			byte[] errorBuffer2 = Encoding.ASCII.GetBytes(reason);
			byte[] errorBuffer3 = new byte[errorBuffer.Length + errorBuffer2.Length + 2];
			Buffer.BlockCopy(errorBuffer, 0, errorBuffer3, 0, errorBuffer.Length);
			Buffer.BlockCopy(errorBuffer2, 0, errorBuffer3, errorBuffer.Length, errorBuffer2.Length);
			errorBuffer3[errorBuffer3.Length - 2] = (byte)'\n';
			errorBuffer3[errorBuffer3.Length - 1] = (byte)'\n';
			/*foreach (byte element in Encoding.ASCII.GetString(errorBuffer3))
			{
				Console.WriteLine("{0} = {1}", element, (char)element);
			}*/
			Console.WriteLine(Encoding.ASCII.GetString(errorBuffer3));
			socket.Send(errorBuffer3, 0);
		}
		public void sendHashReply(Dictionary<String, String> map)
		{
			StringBuilder reply = new StringBuilder();
			reply.Append('\n');
			foreach (KeyValuePair<String, String> pair in map)
			{
				reply.Append(pair.Key);
				reply.Append(':');
				reply.Append(pair.Value);
				reply.Append('\n');
			}
			reply.Append('\n');
			Console.WriteLine(reply.ToString());
			socket.Send(Encoding.UTF8.GetBytes(reply.ToString()));
		}
		public void sendHashReply(List<Dictionary<String, String>> list)
        {
			StringBuilder reply = new StringBuilder();
			reply.Append('\n');
			foreach (Dictionary<String, String> map in list)
			{
				foreach (KeyValuePair<String, String> pair in map)
				{
					reply.Append(pair.Key);
					reply.Append(':');
					reply.Append(pair.Value);
					reply.Append('\n');
				}
			}
			reply.Append('\n');
			Console.WriteLine(reply.ToString());
			socket.Send(Encoding.UTF8.GetBytes(reply.ToString()));
		}
		public void sendData(List<byte> bytes)
        {
			Console.WriteLine("To send :" + bytes.Count);
			int size = Convert.ToInt32(bytes.Count());
			byte[] sizeBytes = BitConverter.GetBytes(size);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(sizeBytes);
			List<byte> toSend = new List<byte>();
			toSend.Add(0);
			toSend.AddRange(sizeBytes);
			toSend.AddRange(bytes);
			Console.WriteLine("Sending : " + toSend.Count);
			socket.Send(toSend.ToArray());
		}
		public void sendOk()
        {
			socket.Send(Encoding.UTF8.GetBytes("\n\n"));
        }
		public void Close()
        {
			socket.Close();
        }
	}


	/*
	 * This class handle the connection and most of NWA protocol
	 */

	public class NWAServer
	{
		NWAClient[] clients;
		List<NWAClient> clientsMemoryAccess;
		Socket			serverSocket;
		private BizHawk.Client.Common.Config _config;
		public Func<String, bool> newClientConnectedCallBack;
		public Func<String, bool> clientDisconnectedCallBack;
		public Func<String, String, bool> newClientNameCallBack;
		public Func<bool>		  serverStartedCallBack;
		public uint numberOfClient = 0;
		public IPEndPoint localEP;

		public NWAServer()
		{
			//_config = config;
			/*CommandHandler._emu = emulator;
			CommandHandler._gameInfo = game;
			CommandHandler._emuClientApi = emuClientApi;*/
			clientsMemoryAccess = new List<NWAClient>();
		}
		internal void addMemoryAccessClient(NWAClient client)
        {
			clientsMemoryAccess.Add(client);
        }
		public void	start()
		{
			clients = new NWAClient[5];
			//IPHostEntry ipHostInfo = Dns.GetHostEntry("localhost");
			//localEP = new IPEndPoint(ipHostInfo.AddressList[0], 65400);
			localEP = new IPEndPoint(IPAddress.IPv6Any, 65400);

			Console.WriteLine($"Local address and port : {localEP.ToString()}");

			serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			serverSocket.DualMode = true;
			while (true)
            {
				try
				{
					serverSocket.Bind(localEP);
					break;
				} catch (Exception ex)
                {
					if (localEP.Port == 65405)
						return;
					localEP.Port += 1;
                }
            }
			serverSocket.Listen(10);
			serverStartedCallBack?.Invoke();
			serverSocket.BeginAccept(new AsyncCallback(this.AcceptCallback), serverSocket);
		}

		public void handleMemoryAccess()
        {
			foreach(NWAClient client in clientsMemoryAccess)
            {
				// Read is the easiest
				Console.WriteLine("Client : " + client.name + "Read memory");
				if (client.writeAccess == false)
                {
					CommandHandler.actualReadMemory(client);
                } else
                {
					if (client.readyToWrite)
                    {
						CommandHandler.actualWriteMemory(client);
                    }
                }
            }
			clientsMemoryAccess.Clear();
        }
		public void doStuffOnFrame()
        {
			handleMemoryAccess();
        }

		private void removeClient(NWAClient client)
		{
			for (uint i = 0; i < clients.Length; i++)
			{
				if (clients[i] == client)
				{
					clients[i] = null;
					clientDisconnectedCallBack?.Invoke(client.name);
				}
			}
			if (clientsMemoryAccess.Contains(client))
			{
				clientsMemoryAccess.Remove(client);
			}
			numberOfClient--;
			
		}
		private void closeAndRemove(NWAClient client)
        {
			client.Close();
			removeClient(client);
        }
		private void AcceptCallback(IAsyncResult ar)
		{
			Socket listener = (Socket) ar.AsyncState;
			Console.WriteLine("New client connected " + numberOfClient);
			if (numberOfClient < 5)
			{
				Socket handler = listener.EndAccept(ar);
				NWAClient newClient = new NWAClient(this);
				newClient.socket = handler;
				numberOfClient++;
				for (int i = 0; i < clients.Length; i++)
				{
					if (clients[i] == null)
						clients[i] = newClient;
				}
				newClientConnectedCallBack?.Invoke(newClient.name);
				handler.BeginReceive(newClient.buffer, 0, NWAClient.BufferSize, SocketFlags.None,
				new AsyncCallback(this.ReadCallback), newClient);
			}
			// This is really dumb to have to readd the callback x)
			serverSocket.BeginAccept(new AsyncCallback(this.AcceptCallback), serverSocket);
		}

		private void ReadCallback(IAsyncResult ar)
		{
			NWAClient client = (NWAClient) ar.AsyncState;
			try
			{
				client.readed = client.socket.EndReceive(ar);
			} catch(Exception ex)
            {
				Console.WriteLine(ex.ToString());
				closeAndRemove(client);
				return;
            }
			Console.WriteLine("Readed : {0}", client.readed);
			//Console.WriteLine("Recevied data on the client : {0}", client.buffer);
			if (client.readed != 0)
				handleReadData(client);
			else
            {
				Console.WriteLine("Client closed the connection : " + client.name);
				closeAndRemove(client);
				return;
            }
			if (client.socket.Connected)
			{
				client.socket.BeginReceive(client.buffer, 0, NWAClient.BufferSize, SocketFlags.None,
				new AsyncCallback(this.ReadCallback), client);
			}
		}

		private void handleReadData(NWAClient client)
		{
			int pos = 0;
			while (pos < client.readed)
			{
				Console.WriteLine("Client in state {0}", client.state);
				if (client.state == NWAClient.State.ExpectingBinaryData)
				{
					client.state = NWAClient.State.GettingBinaryData;
					if (client.buffer[pos] != 0)
					{
						client.sendError(ErrorKind.protocol_error, "Expected binary block");
						closeAndRemove(client);
						return;
					}
					pos++;
					client.binaryHeaderPos = 0;
				}
				if (client.state == NWAClient.State.GettingBinaryData)
				{
					if (client.binaryHeaderPos < 4)
                    {
						while (pos < client.readed)
                        {
							client.binaryHeader[client.binaryHeaderPos] = client.buffer[pos];
							pos++;
							client.binaryHeaderPos++;
							if (client.binaryHeaderPos == 4)
								break;
                        }
						if (client.binaryHeaderPos == 4)
						{
							long size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(client.binaryHeader, 0));
							if (size != client.expectedBinaryDataSize && client.expectedBinaryDataSize > 0)
                            {
								client.sendError(ErrorKind.protocol_error, "Expecting a binary block of size " + client.expectedBinaryDataSize + " received a size of " + size);
								closeAndRemove(client);
								return;
							}
							client.binaryBuffer = new byte[size];
							Console.WriteLine("New buffer size : " + size + " L " + client.binaryBuffer.Length);
							client.binaryBufferPos = 0;
						}
					}
					if (client.binaryHeaderPos == 4)
					{
						Console.WriteLine("Expected size : " + client.expectedBinaryDataSize + " Buffer size :" + client.binaryBuffer.Length);
						int startPos = pos;
						for (; pos - startPos < client.binaryBuffer.Length && pos < client.readed; pos++)
						{
							client.binaryBuffer[client.binaryBufferPos] = client.buffer[pos];
							client.binaryBufferPos++;
						}
						if (client.binaryBufferPos == client.binaryBuffer.Length)
						{
							processBinaryData(client);
						}
					}
					continue;
				}
				if (client.state == NWAClient.State.WaitingForCommand)
				{
					if (client.buffer[pos] == 0)
					{
						client.sendError(ErrorKind.protocol_error, "Invalid data sent when waiting for a command");
						closeAndRemove(client);
						return;
					}
					if (Char.IsLetter(Convert.ToChar(client.buffer[pos])) || client.buffer[pos] == '!')
					{
						client.state = NWAClient.State.GettingCommand;
					}
					else
					{
						client.sendError(ErrorKind.protocol_error, "Invalid data sent when waiting for a command");
						closeAndRemove(client);
						return;
						//TODO close conneciton?
					}
				}
				if (client.state == NWAClient.State.GettingCommand)
				{
					if (client.buffer.Contains(Convert.ToByte('\n')))
					{
						int startingPos = pos;
						for (; client.buffer[pos] != '\n'; pos++)
						{
							client.bufferCommandPos++;
							client.bufferCommand[client.bufferCommandPos + pos - startingPos] = client.buffer[pos];
						}
						executeCommand(client);
						pos++;
					}
					else
					{ // We did not get all the command
						int startingPos = pos;
						for (; pos < client.readed; pos++)
						{
							client.bufferCommand[client.bufferCommandPos + pos - startingPos] = client.buffer[pos];
							client.bufferCommandPos++;
						}
					}
				}
				Array.Clear(client.buffer, 0, pos); // This is to remove a previous \n, make the code a bit simplier
			}
			Array.Clear(client.buffer, 0, NWAClient.BufferSize);
		}
		private void executeCommand(NWAClient client)
		{
			byte[] copy = new byte[client.bufferCommandPos];
			Array.Copy(client.buffer, copy, client.bufferCommandPos);
			string commandString = Encoding.UTF8.GetString(copy);
			Console.WriteLine("Trying to process : {0}$", commandString);
			string command;
			string[] args = null;
			if (commandString.Contains(' '))
			{
				command = commandString.Split(' ')[0];
				commandString = commandString.Remove(0, command.Length + 1);
				args = commandString.Split(';'); //.Split(' ', 2)[1].Split(';'); // FIXME, We need the whole second string
			} else
			{
				command = commandString;
			}
			Console.WriteLine("Command extracted is : {0}$", command);
			NWACommand nwaCommand;
			if (Enum.TryParse<NWACommand>(command.ToString(), out nwaCommand) &&
				CommandHandler.commandsMap.ContainsKey(nwaCommand))
			{
				Console.WriteLine("Executing : {0}", nwaCommand);
				CommandHandler.commandsMap[nwaCommand](client, args);
				client.currentCommand = nwaCommand;
			} else {
				client.sendError(ErrorKind.invalid_command, "Unknow command : " + command);
			}
			if (command[0] == 'b')
            {
				client.shallowBinaryBlock = false;
				client.state = NWAClient.State.ExpectingBinaryData;
				if (client.expectedBinaryDataSize == -1)
					client.shallowBinaryBlock = true;
            } else {
				client.state = NWAClient.State.WaitingForCommand;
			}
			client.bufferCommandPos = 0;
			Array.Clear(client.bufferCommand, 0, NWAClient.BufferCommandSize);
		}
		private void processBinaryData(NWAClient client)
		{
			Console.WriteLine("Processing Binary data");
			if (!client.shallowBinaryBlock)
            {
				CommandHandler.binaryCommandHandlerMap[client.currentCommand](client);
            }
			client.state = NWAClient.State.WaitingForCommand;
		}
	}
}
