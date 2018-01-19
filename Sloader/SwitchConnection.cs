using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Sloader
{
    class SwitchConnection : IDisposable
    {
        //private string m_ConnType;
        private ConnectionInfo m_ConnNfo;
        private SshClient _client = null;
        private string _SwitchType;
        private ShellStream _shellStream;
        private ShellStream ShellStream
        {
            get
            {
                if (_shellStream == null)
                {
                    _shellStream = _client.CreateShellStream("stun", 80, 24, 800, 600, 1024);
                    _shellStream.DataReceived += _shellStream_DataReceived;
                }
                return _shellStream;
            }
        }

        void _shellStream_DataReceived(object sender, ShellDataEventArgs e)
        {
            var text = FromBytes(e.Data);
            ConnectionMessageBuffer.AppendMessage(text);
            Console.WriteLine(text);
            //m_DisplayText.AppendText(text);  //can't update the UI from this thread
        }

        private readonly EolOptions _eol;
        public enum EolOptions
        {
            Char13,
            Char10,
            Char1013,
            None
        }

        string _username;
        string _host;
        int _port;

        public SwitchConnection(string user, string pass, string ip, int prt, string switchType)
        {
            _username = user;
            string password = pass;
            _host = ip;
            _port = prt;
            _SwitchType = switchType;
 
            // Setup Credentials and Server Information
            var kiam = new KeyboardInteractiveAuthenticationMethod(_username);
            var pam = new PasswordAuthenticationMethod(_username, password.Trim());
            m_ConnNfo = new ConnectionInfo(_host, _port, _username, kiam, pam);
            m_ConnNfo.Encoding = Encoding.ASCII;

            kiam.AuthenticationPrompt += delegate(object o, AuthenticationPromptEventArgs ev)
            {
                foreach (var prompt in ev.Prompts)
                {
                    if (prompt.Request.Equals("Password: ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        prompt.Response = password.Trim(); // "Password" acquired from resource
                    }
                }
            };
        }

        public bool Connected
        {
            get
            {
                return _client.IsConnected;
            }
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                if (_client.IsConnected)
                {
                    //if (TelebossConnected)
                    //{
                    //    //Log off the switch first CM6 Specific..should probably put this with the switch type somewhere and call the proper logoff command
                    //    SendCommand("logoff");
                    //    TelebossConnected = false;
                    //}
                    _client.Disconnect();
                }
            }
        }

        public void Connect()
        {
            switch (_SwitchType)
            {
                case "AVAYA":
                    ConnectDirectToAvayaSwitch();
                    break;
                case "TELEBOSS":
                    ConnectDirectToTeleboss();
                    break;
                default:
                    return;
            }            
        }

        private void ConnectDirectToTeleboss()
        {
            Console.WriteLine("Attempting to connect to {0}", _host);
            _client = new SshClient(m_ConnNfo);
            _client.Connect();           
        }

        private void ConnectDirectToAvayaSwitch()
        {
            Console.WriteLine("attempting to connect to {0}", _host);
            _client = new SshClient(m_ConnNfo);
            _client.Connect();
            //cm6 specific I think...

            var promptwas = this.ShellStream.Expect("Terminal Type");
            
            this.SendCommand("VT220");  //Using VT220 because that's what our cairs parsing tool uses.
        }

        public void Wait(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public string SendCommand(string command, bool echo = true, bool sendEndOfLine = true, bool clearBuffer = true)
        {
            if (clearBuffer) { ConnectionMessageBuffer.Clear(); }

            var cmd = command.Trim();
            
            try
            {
                if (sendEndOfLine)
                    ShellStream.Write(cmd + EndOfLineChar);
                else
                    ShellStream.Write(cmd);
                if (echo) Console.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return "";
            }
            return "";
        }
        
        readonly MessageBuffer _messageBuffer = new MessageBuffer();

        public MessageBuffer ConnectionMessageBuffer
        {
            get
            {
                return _messageBuffer;
            }
        }

        string EndOfLineChar
        {
            get
            {
                switch (this._eol)
                {
                    case EolOptions.Char13:
                        return "\r";
                    case EolOptions.Char10:
                        return "\n";
                    case EolOptions.Char1013:
                        return "\n\r";
                    case EolOptions.None:
                        return "";
                    default:
                        return "\r";
                }
            }
        }

        string FromBytes(byte[] bytes)
        {
            var text = this._client.ConnectionInfo.Encoding.GetString(bytes);
            return text;
        }

        public void Dispose()
        {
            //TO DO: CLOSE PORT
        }
    }

    public class MessageBuffer
    {
        private StringBuilder currentMessage = new StringBuilder(1024);
        public string CurrentMessage { get { return currentMessage.ToString(); } }
        public void Clear() { currentMessage.Clear(); }
        public void SetMessage(string message)
        {
            currentMessage.Clear();
            currentMessage.Append(message);
        }
        public void AppendMessage(string message) { currentMessage.Append(message); }
        public void AppendMessage(char message) { currentMessage.Append(message); }
    }
}

