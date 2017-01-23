using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections;

namespace ChatServer
{

    public class StatusChangedEventArgs : EventArgs
    {

        private string EventMsg;
  


        public string EventMessage
        {
            get
            {
                return EventMsg;
            }
            set
            {
                EventMsg = value;
            }
        }

        public StatusChangedEventArgs(string strEventMsg)
        {
            EventMsg = strEventMsg;
        }
    }

    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    class ChatServer
    {
  
        public static Hashtable htUsers = new Hashtable(30); 

        public static Hashtable htConnections = new Hashtable(30); 
      
        private IPAddress ipAddress;
        private TcpClient tcpClient;

        private static RSA rsa_send = new RSA();

        public static event StatusChangedEventHandler StatusChanged;
        private static StatusChangedEventArgs e;
        private static string name_server;
       
        public ChatServer(IPAddress address)
        {
            ipAddress = address;
        }

        public void set_name_server(string name)
        {
            name_server = name;
        }
        public string get_name_server()
        {
            return name_server;
        }

        public static RSA get_rsa_send()
        {
            return rsa_send;
        }
        private Thread thrListener;
        private TcpListener tlsClient;

        
        bool ServRunning = false;

       
        public static void AddUser(TcpClient tcpUser, string strUsername)
        {
            
            ChatServer.htUsers.Add(strUsername, tcpUser);
            ChatServer.htConnections.Add(tcpUser, strUsername);
            
            send_keys(rsa_send.GetNKey() , rsa_send.GetEKey());

            SendAdminMessage(htConnections[tcpUser] + " has joined us");

        }
        private static void send_keys(ushort n_key, ushort e_key)
        {
            SendMessage(name_server, n_key.ToString() + ' ' + e_key.ToString());
        }

       
        public static void RemoveUser(TcpClient tcpUser)
        {
          
            if (htConnections[tcpUser] != null)
            {            
                SendAdminMessage(htConnections[tcpUser] + " has left us");
                ChatServer.htUsers.Remove(ChatServer.htConnections[tcpUser]);
                ChatServer.htConnections.Remove(tcpUser);
            }
        }

        
        public static void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChangedEventHandler statusHandler = StatusChanged;
            if (statusHandler != null)
            {
               
                statusHandler(null, e);
            }
        }

        
        public static void SendAdminMessage(string Message)
        {
            StreamWriter swSenderSender;
            e = new StatusChangedEventArgs("Administrator : " + Message);
            OnStatusChanged(e);

            TcpClient[] tcpClients = new TcpClient[ChatServer.htUsers.Count];
    
            ChatServer.htUsers.Values.CopyTo(tcpClients, 0);
            
            for (int i = 0; i < tcpClients.Length; i++)
            {
                try
                {
                    if (Message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }
                   
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine("Administrator : " + Message);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }

        
        public static void SendMessage(string From, string Message)
        {

            StreamWriter swSenderSender;
            
            e = new StatusChangedEventArgs(From + " : " + Message);
            OnStatusChanged(e);

           
            TcpClient[] tcpClients = new TcpClient[ChatServer.htUsers.Count];
            
            ChatServer.htUsers.Values.CopyTo(tcpClients, 0);
            
            for (int i = 0; i < tcpClients.Length; i++)
            {
                
                try
                {
                    
                    if (Message.Trim() == "" || tcpClients[i] == null)
                    {
                        continue;
                    }
                    
                    swSenderSender = new StreamWriter(tcpClients[i].GetStream());
                    swSenderSender.WriteLine(From + " : " + Message);
                    swSenderSender.Flush();
                    swSenderSender = null;
                }
                catch 
                {
                    RemoveUser(tcpClients[i]);
                }
            }
        }

        public void StartListening()
        {          
            IPAddress ipaLocal = ipAddress;          
            tlsClient = new TcpListener(1986);    
            tlsClient.Start();
            ServRunning = true;  
            thrListener = new Thread(KeepListening);
            thrListener.Start();
        }

        private void KeepListening()
        {
            while (ServRunning == true)
            {
                
                tcpClient = tlsClient.AcceptTcpClient();
                
                Connection newConnection = new Connection(tcpClient);
            }
        }
    }

    

}
