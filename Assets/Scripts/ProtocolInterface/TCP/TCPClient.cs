using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPClient : IClientProtocol
{

    private Socket tcpClient;
    private Thread listenerThread;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private bool connected;
    private OnConnectionFailure onConnectionFailure;
    private OnServerDisconneced onDisconnected;
    private OnConnectedDelegate onConnected;
    private EndPoint serverEndPoint;

    public TCPClient(int port,EndPoint serverEndPoint)// o puerto aleatorio como en MUSE-RP
    {
        tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        tcpClient.Bind(localEndPoint);
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        connected = false;
        this.serverEndPoint = serverEndPoint;
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

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        this.onConnectionFailure += onConnectionFailure;
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        this.onDisconnected += onDisconnected;
    }

    public void OnAppQuit()
    {
        tcpClient.Close();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        this.onConnected += onConnected;

    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void RemoveOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        this.onConnectionFailure -= onConnectionFailure;
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        this.onDisconnected -= onDisconnected;
    }

    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();
      
        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        tcpClient.Send(bytesToSend.ToArray());
    }

    public void TryConnect()
    {
        try
        {
            tcpClient.Connect(serverEndPoint);
            onConnected?.Invoke();
            listenerThread = new Thread(() => ListeningThread());
            listenerThread.Start();
            SendToServer(0, null, true);
        }
        catch (Exception e)
        {
            Debug.LogError("Excepcion conectando: " + e.Message);
            onConnectionFailure?.Invoke();
        }

    }
    private bool IsConnected()
    {
        try
        {
            return !(tcpClient.Poll(1, SelectMode.SelectRead) && tcpClient.Available == 0);
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
            size = tcpClient.ReceiveFrom(buffer, ref endPoint);
            if (endPoint.Equals(serverEndPoint))
            {
                ushort type = BitConverter.ToUInt16(buffer, 0);
                if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                {
                    value?.Invoke(buffer);
                }
            }
        }
        onDisconnected?.Invoke();
    }
    
 
}
