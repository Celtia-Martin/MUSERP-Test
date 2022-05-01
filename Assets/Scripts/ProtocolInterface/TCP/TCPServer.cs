using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPServer : IServerProtocol
{
    private Dictionary<string, EndPoint> clients;
    private Thread listenerThread;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private OnServerDisconneced onDisconnected;
    private OnClientConnectedDelegate onConnected;
    private OnClientDisconnected disconnectedDelegate;
    private Socket tcpServer;
    private int clientID;
    public TCPServer(int port)// o puerto aleatorio como en MUSE-RP
    {
        tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        tcpServer.Bind(localEndPoint);
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        clients = new Dictionary<string, EndPoint>();
  
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
        listenerThread = new Thread(() => ListeningThread());
        listenerThread.Start();
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
        tcpServer.SendTo(bytesToSend.ToArray(), conn.endPoint);
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        foreach ( EndPoint endpoint in clients.Values)
        {
            tcpServer.SendTo(bytesToSend.ToArray(), endpoint);
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
    private void ListeningThread()
    {
        byte[] buffer = new byte[2000];
        int size;
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (IsConnected())
        {
            try
            {
                size = tcpServer.ReceiveFrom(buffer, ref endPoint);
                IPEndPoint ipEndPoint = endPoint as IPEndPoint;
                ushort type = BitConverter.ToUInt16(buffer, 0);
                if (clients.ContainsKey(ipEndPoint.Port + ipEndPoint.Address.ToString()))
                {
                    if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                    {
                        value?.Invoke(buffer);
                    }
                }
                else if (type == 0)
                {
                    ClientConnected(ipEndPoint);
                }
            }
           catch(Exception e)
            {
                Console.instance.WriteLine("Error in server: " + e.Message);
            }
        }
        onDisconnected?.Invoke();
    }
    private void ClientConnected(IPEndPoint clientEndPoint)
    {
        try
        {
            clients.Add(clientEndPoint.Port + clientEndPoint.Address.ToString(), clientEndPoint);
        }catch(Exception e)
        {
            return;
        }
        clientID++;
        onConnected?.Invoke(new ConnectionInfo(clientEndPoint.Address.ToString(), clientEndPoint.Port,0,clientID));
    }
}
