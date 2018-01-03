using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;

namespace eagle.tunnel.dotnet.core
{
    public class Server
    {
        public Server()
        {
            ;
        }

        public bool Start(int port)
        {
            try
            {
                string host = Dns.GetHostName();
                IPAddress[] ipas = Dns.GetHostAddresses(host);
                foreach (IPAddress ipa in ipas)
                {
                    if(ipa.AddressFamily == AddressFamily.InterNetwork)
                    {
                        TcpListener server = new TcpListener(ipa, port);
                        server.Start();
                        Thread handleServerThread = new Thread(HandleServer);
                        handleServerThread.IsBackground = true;
                        handleServerThread.Start(server);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private void HandleServer(object serverObj)
        {
            TcpListener server = serverObj as TcpListener;
            while(true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread handleClientThread = new Thread(HandleClient);
                handleClientThread.IsBackground = true;
                handleClientThread.Start(client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient socket2Client = clientObj as TcpClient;
            NetworkStream stream2client = socket2Client.GetStream();
            HandleSocket2Client(stream2client);
        }

        private void HandleSocket2Client(NetworkStream stream2client)
        {
            byte[] buffer = new byte[102400];
            try
            {
                int count = stream2client.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, count);
                string url = GetURL(request);
                if(url == "")
                {
                    return ;
                }
                IPAddress[] ipas = Dns.GetHostAddresses(url);
                string ip = ipas[0].ToString();
                Console.WriteLine("connect to " + url + " " + ip);
                
                TcpClient client2Server = new TcpClient(ip, 80);
                NetworkStream stream2server = client2Server.GetStream();
                
                Pipe pipe0 = new Pipe(stream2client, stream2server);
                Pipe pipe1 = new Pipe(stream2server, stream2client);

                pipe0.Flow();
                pipe1.Flow();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private string GetURL(string request)
        {
            Console.WriteLine("Request: ");
            Console.WriteLine(request);
            StringReader reader = new StringReader(request);
            string line = reader.ReadLine();
            while(line != null)
            {
                if(line.Contains("Host:"))
                {
                    int ind = 6;
                    string url = line.Substring(ind);
                    Console.WriteLine("URL found: " + url);
                    return url;
                }
                else
                {
                    line = reader.ReadLine();
                }
            }
            Console.WriteLine("URL not found");
            return "";
        }
    }
}