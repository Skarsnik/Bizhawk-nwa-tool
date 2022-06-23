using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;

namespace Nyo.Fr.EmuNWA
{

    [ExternalTool("NWATool")]
    public sealed partial class NWAToolForm : ToolFormBase, IExternalToolForm
    {
        public ApiContainer? _maybeAPIContainer { get; set; }

        private ApiContainer APIs => _maybeAPIContainer!;

        protected override string WindowTitleStatic => "Emulator Network Access";
        private NWAServer _server;
        List<string> messages = new List<string>();

        public NWAToolForm()
        {
            InitializeComponent();
            _server = new NWAServer();
            _server.serverStartedCallBack = serverStarted;
            _server.newClientConnectedCallBack = clientConnected;
            _server.newClientNameCallBack = clientNameChanged;
            _server.clientDisconnectedCallBack = clientDisconnected;
            _server.start();
        }
        private void addMessage(string msg)
        {
            if (APIs == null)
                messages.Add(msg);
            else
                APIs.Gui.AddMessage(msg);
        }
        private bool serverStarted()
        {
            addMessage("NWA Server started");
            ServerStatusLabel.Text = "Server started succesfully, listening on " + _server.localEP.Address.ToString() + " port : " + _server.localEP.Port;
            return true;
        }
        private bool clientConnected(string name)
        {
            if (ClientsListView.InvokeRequired)
            {
                System.Action safeWrite = delegate { clientConnected(name); };
                ClientsListView.Invoke(safeWrite);
            }
            else
            {
                ClientsListView.Items.Add(name).Name = name;
                addMessage("New NWA Client connected");
            }
            return true;
        }
        private bool clientNameChanged(string oldname, string name)
        {
            if (ClientsListView.InvokeRequired)
            {
                System.Action safeWrite = delegate { clientNameChanged(oldname, name); };
                ClientsListView.Invoke(safeWrite);
            }
            else
            {
                var items = ClientsListView.Items.Find(oldname, false);
                items[0].Text = name;
                items[0].Name = name;
                addMessage("NWA Client renamed to " + name);
            }
            return true;
        }
        private bool clientDisconnected(string name)
        {
            if (ClientsListView.InvokeRequired)
            {
                System.Action safeWrite = delegate { clientDisconnected(name); };
                ClientsListView.Invoke(safeWrite);
            }
            else
            {
                ClientsListView.Items.RemoveByKey(name);
                addMessage("NWA Client " + name + " disconnected");
            }
            return true;
        }
        public override void Restart()
        {
            CommandHandler.APIs = APIs;
            if (messages.Count != 0)
            {
                foreach (var message in messages)
                    APIs.Gui.AddMessage(message);
                messages.Clear();
            }
        }
        protected override void UpdateAfter()
        {
            _server.doStuffOnFrame();
            //System.Console.WriteLine("Update after");
        }
        protected override void FastUpdateAfter()
        {
            _server.doStuffOnFrame();
        }
    }
}