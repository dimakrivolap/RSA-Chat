using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace ChatClient
{
    public partial class frmChatClient : Form
    {
        private string UserName = "Unknown";
        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;

        private delegate void UpdateLogCallback(string strMessage);

        private delegate void CloseConnectionCallback(string strReason);
        private Thread thrMessaging;
        private IPAddress ipAddr;
        private bool Connected;
        private static RSA rsa_receive;
        private static RSA rsa_send = new RSA();

        public static string my_last_message = "";
        public static string nameServer = "";

        public frmChatClient()
        {
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();
        }

        public void OnApplicationExit(object sender, EventArgs e)
        {
            if (Connected == true)
            {
                Connected = false;
                swSender.Close();
                srReceiver.Close();
                tcpServer.Close();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if ((String.Compare(txtUser.Text, "") != 0)&&(String.Compare(txtIp.Text, "") != 0))
            {
                if (Connected == false)
                {
                    InitializeConnection();
                    send_keys(rsa_send.GetNKey(), rsa_send.GetEKey());
                }
                else
                {
                    CloseConnection("Disconnected at user's request.");
                }
            }
            else
            {
                MessageBox.Show("Неверный Ввод!");
            }
        }

        private void InitializeConnection()
        {
            ipAddr = IPAddress.Parse(txtIp.Text);
            tcpServer = new TcpClient();
            tcpServer.Connect(ipAddr, 1986);

            Connected = true;
            UserName = txtUser.Text;

            txtIp.Enabled = false;
            txtUser.Enabled = false;
            txtMessage.Enabled = true;
            btnSend.Enabled = true;
            btnConnect.Text = "Отключиться";

            swSender = new StreamWriter(tcpServer.GetStream());
            swSender.WriteLine(txtUser.Text);
            swSender.Flush();
            thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
            thrMessaging.Start();

            
        }

        private void ReceiveMessages()
        {
            srReceiver = new StreamReader(tcpServer.GetStream());
            string ConResponse = srReceiver.ReadLine();
            if (ConResponse[0] == '1')
            {
                this.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { "Connected Successfully!" });
            }
            else 
            {
                string Reason = "Not Connected: ";
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                this.Invoke(new CloseConnectionCallback(this.CloseConnection), new object[] { Reason });
                return;
            }
            while (Connected)
            {
                try
                {
                    this.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { srReceiver.ReadLine() });
                }
                catch(Exception e)
                {
                    Environment.Exit(0);
                }
            }
        }

        private void UpdateLog(string strMessage)
        {
            int i=0;
            string name="";
            while (strMessage[i] != ' ')
            {
                name = name + strMessage[i];
                i++;
            }
            if ((String.Compare(nameServer, name) == 0) || (nameServer == "") || (String.Compare(txtUser.Text, name) == 0) || (String.Compare("Administrator", name) == 0))
            {
                i += 3;
                int data_pos = i;
                Regex regex = new Regex("[0-9]{1,5} [0-9]{1,5}");
                if ((regex.IsMatch(strMessage)) && (String.Compare(UserName, name) != 0))
                {
                    if (nameServer == "")
                        nameServer = name;

                    string n_key = "", e_key = "";

                    while (strMessage[i] != ' ')
                    {
                        n_key = n_key + strMessage[i];
                        i++;
                    }
                    i++;
                    while (i < strMessage.Length)
                    {
                        e_key = e_key + strMessage[i];
                        i++;
                    }
                    rsa_receive = new RSA(ushort.Parse(n_key), ushort.Parse(e_key));
                }
                else if ((regex.IsMatch(strMessage)) && (String.Compare(UserName, name) == 0))
                {

                }
                else if (!(regex.IsMatch(strMessage)) && (String.Compare(txtUser.Text, name) == 0))
                {
                    txtLog.AppendText(name + " : " + my_last_message + "\r\n");
                }
                else
                {
                    string n_s = rsa_send.GetNKey().ToString();
                    string d_s = rsa_send.GetDKey().ToString();

                    string mes = strMessage.Substring(data_pos, strMessage.Length - data_pos);
                    if (String.Compare("Administrator", name) != 0)
                        mes = rsa_send.decode(mes, n_s, d_s);
                    txtLog.AppendText(name + " : " + mes + "\r\n");
                }
            }
        }

        private void CloseConnection(string Reason)
        {
            txtLog.AppendText(Reason + "\r\n");
            txtIp.Enabled = true;
            txtUser.Enabled = true;
            txtMessage.Enabled = false;
            btnSend.Enabled = false;
            btnConnect.Text = "Connect";
            Connected = false;
            swSender.Close();
            srReceiver.Close();
            tcpServer.Close();
        }
        private void send_keys(ushort n_key, ushort e_key)
        {
            swSender.WriteLine(n_key.ToString() + ' ' + e_key.ToString());
            txtMessage.Text = "";
            swSender.Flush();
        }

        private void SendMessage()
        {
            if (txtMessage.Lines.Length >= 1)
            {
                my_last_message = txtMessage.Text;
                string mes = rsa_receive.encode(txtMessage.Text);
                swSender.WriteLine(mes);
                swSender.Flush();
                txtMessage.Lines = null;
                rsa_send = new RSA();
                send_keys(rsa_send.GetNKey(), rsa_send.GetEKey());
            }
            txtMessage.Text = "";
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                SendMessage(); 
            }
        }



    }
}