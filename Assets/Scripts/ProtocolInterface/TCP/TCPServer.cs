using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPServer : IServerProtocol
{
    private Dictionary<string, Socket> clients;
    private Thread[] listenerThread;
    private Thread openingConnectionThread;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private OnServerDisconneced onDisconnected;
    private OnClientConnectedDelegate onConnected;
    private OnClientDisconnected disconnectedDelegate;
    private Socket tcpServer;
    private int clientID;
    private int maxConnections;
    public TCPServer(int port,int maxConnections)// o puerto aleatorio como en MUSE-RP https://stackoverflow.com/questions/36526332/simple-socket-server-in-unity
    {
        tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        tcpServer.Bind(localEndPoint);
        tcpServer.NoDelay = true;
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        clients = new Dictionary<string, Socket>();
        this.maxConnections = maxConnections;
        listenerThread = new Thread[maxConnections];
    }
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        handlerDictionary.Add(type, (b) =>
        {
            byte[] data = new byte[b.Length - 2];
            Array.Copy(b, 2, data, 0, b.Length - 2);
            ushort type = BitConverter.ToUInt16(b, 0);
            handler?.Invoke(new MessageObject(type, 0, 0, false, false, false, false, data), null);
        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        handlerDictionary.Add(type, handler);
    }

    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        this.onConnected += connectedDelegate;
        this.disconnectedDelegate += disconnectedDelegate;
    }

    public void OnAppQuit()
    {
        tcpServer.Close();
    }

    public void OnStart()
    {
        openingConnectionThread = new Thread(() => OpenConnectionThread());
        openingConnectionThread.Start();

    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void SendEndToAll()
    {
       // throw new NotImplementedException();
    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        throw new NotImplementedException();
    }

    public void SendTo(ushort type, byte[] message, Connection conn, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        if(clients.TryGetValue(conn.port+conn.IP,out Socket handler))
        {
            bytesToSend.AddRange(BitConverter.GetBytes('!'));
            handler.Send(bytesToSend.ToArray());
            Debug.Log(bytesToSend.Count + " " + handler.NoDelay);
        }
        //if (message != null)
        //{
        //    bytesToSend.AddRange(message);
        //    tcpServer.SendTo(bytesToSend.ToArray(), conn.endPoint);
        //}
        //else
        //{
        //    tcpServer.SendTo(null, conn.endPoint);
        //}
     
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        bytesToSend.AddRange(BitConverter.GetBytes('!'));
        foreach ( Socket socket in clients.Values)
        {
           
            socket.Send(bytesToSend.ToArray());
           
        }
    }
    private bool IsConnected()
    {
        try
        {
            return !(tcpServer.Poll(1, SelectMode.SelectRead) && tcpServer.Available == 0);
        }
        catch (SocketException) { return false; }
    }
    private void ListeningThread(Socket socket)
    {
        byte[] buffer = new byte[1024];
        int size;
        while (IsConnected())
        {
            try
            {
                //size = tcpServer.ReceiveFrom(buffer, ref endPoint);
                size= socket.Receive(buffer);
               
                if (clients.ContainsKey((socket.RemoteEndPoint as IPEndPoint).Port + (socket.RemoteEndPoint as IPEndPoint).Address.ToString()))
                {
                    string stream = System.Text.Encoding.ASCII.GetString(buffer.Take(size).ToArray());
                    string[] messages = stream.Split('!');
                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        if (messages[0].Length > 0)
                        {
                            byte[] messageData = System.Text.Encoding.ASCII.GetBytes(messages[0]);
                            ushort type = BitConverter.ToUInt16(messageData, 0);
                            if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                            {
                                value?.Invoke(messageData);
                            }
                        }
                    }
                }
            }
           catch(Exception e)
            {
                Debug.LogError("Error in server: " + e.Message);
                //socket.Close();
            }
        }
        onDisconnected?.Invoke();
    }
    private void OpenConnectionThread()
    {
        byte[] buffer = new byte[1024];
        int size;
        tcpServer.Listen(maxConnections);
        while (IsConnected())
        {
            try
            {
                var handler = tcpServer.Accept();
                ClientConnected(handler);
            }
            catch(Exception e)
            {
                Debug.LogError("Error in server: " + e.Message);
                onDisconnected?.Invoke();
            }
        }
    }
    private void ClientConnected(Socket clientSocket)
    {
        try
        {
           
            IPEndPoint endpoint = clientSocket.RemoteEndPoint as IPEndPoint;
            clients.Add(endpoint.Port + endpoint.Address.ToString(), clientSocket);
            clientSocket.NoDelay = true;
            listenerThread[clientID]= new Thread(() => ListeningThread(clientSocket));
            listenerThread[clientID].Start();
            clientID++;
            onConnected?.Invoke(new ConnectionInfo(endpoint.Address.ToString(), endpoint.Port, 0, clientID));
        }
        catch(Exception e)
        {
            return;
        }
      
    }
}
