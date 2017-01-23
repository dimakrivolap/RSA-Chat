using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace ChatServer
{
    public partial class frmChatServer : Form
    {
        private delegate void UpdateStatusCallback(string strMessage);
        
        public frmChatServer()
        {
            InitializeComponent();
        }
        private StreamWriter swSender;
        private TcpClient tcpServer;

        private static RSA rsa_receive;
        private static RSA rsa_send = new RSA();
        public static string nameClient="";
        public static string my_last_message="";
        private void btnListen_Click(object sender, EventArgs e)
        {
    
                if ((String.Compare(txtUser.Text, "") != 0) && (String.Compare(txtIp.Text, "") != 0))
                {

                    IPAddress ipAddr = IPAddress.Parse(txtIp.Text);

                    ChatServer mainServer = new ChatServer(ipAddr);

                    ChatServer.StatusChanged += new StatusChangedEventHandler(mainServer_StatusChanged);

                    mainServer.StartListening();


                    rsa_send = ChatServer.get_rsa_send();

                    txtLog.AppendText("Monitoring for connections...\r\n");

                    ipAddr = IPAddress.Parse(txtIp.Text);

                    tcpServer = new TcpClient();
                    tcpServer.Connect(ipAddr, 1986);

                    swSender = new StreamWriter(tcpServer.GetStream());
                    swSender.WriteLine(txtUser.Text);
                    swSender.Flush();

                    mainServer.set_name_server(txtUser.Text);
                    btnendlisten.Enabled = true;
                    btnListen.Enabled = false;
                    txtIp.Enabled = false;
                    txtMessage.Enabled = true;
                    btnSend.Enabled = true;
                    txtUser.Enabled = false;
                }
                else
                {
                    MessageBox.Show("Неверный Ввод!");
                }
            
        }

        public void mainServer_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            try
            {
                this.Invoke(new UpdateStatusCallback(this.UpdateStatus), new object[] { e.EventMessage });
            }
            catch
            {
                tcpServer.Close();
                swSender.Dispose();
                swSender.Close();
                this.Dispose();
                Environment.Exit(0);
            
            }
        }

        private void UpdateStatus(string strMessage)
        {
            int i=0;
            string name="";
            while (strMessage[i] != ' ')
            {
                name = name + strMessage[i];
                i++;
            }
            if ((String.Compare(nameClient, name) == 0) || (nameClient == "") || (String.Compare(txtUser.Text, name) == 0)||(String.Compare("Administrator", name) == 0))
            {
                i += 3;
                int data_pos = i;
                Regex regex = new Regex("[0-9]{1,5} [0-9]{1,5}");
                if ((regex.IsMatch(strMessage)) && (String.Compare(txtUser.Text, name) != 0))
                {
                    if (nameClient == "")
                        nameClient = name;

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
                else if ((regex.IsMatch(strMessage)) && (String.Compare(txtUser.Text, name) == 0))
                {

                }
                else if (!(regex.IsMatch(strMessage)) && (String.Compare(txtUser.Text, name) == 0))
                {
                    txtLog.AppendText(name + " : " + my_last_message + "\r\n"); 
                }
                else if (String.Compare("Conected", name) == 0)
                {
                
                    send_keys(rsa_send.GetNKey(), rsa_send.GetEKey());
                }

                else
                {
                    string n_s = rsa_send.GetNKey().ToString();
                    string d_s = rsa_send.GetDKey().ToString();


                    string mes = strMessage.Substring(data_pos, strMessage.Length - data_pos);
                    if (String.Compare("Administrator", name) != 0)
                        mes = rsa_send.decode(mes, n_s, d_s);
                    txtLog.AppendText(name +" : "+ mes + "\r\n");
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
           
            SendMessage();
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
            }
            txtMessage.Text = "";
            rsa_send = new RSA();
            send_keys(rsa_send.GetNKey(), rsa_send.GetEKey());
        }

        private void send_keys(ushort n_key, ushort e_key)
        {
            swSender.WriteLine(n_key.ToString() + ' ' + e_key.ToString());
            txtMessage.Text = "";
            swSender.Flush();
        }

        private void btnendlisten_Click(object sender, EventArgs e)
        {
            tcpServer.Close();
            swSender.Dispose();
            swSender.Close();
            this.Dispose();
            Environment.Exit(0);
        }

        private void txtMessage_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                SendMessage();
            }
        }
    }
}