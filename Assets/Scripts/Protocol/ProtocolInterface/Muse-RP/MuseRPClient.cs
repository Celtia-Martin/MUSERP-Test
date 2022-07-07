using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Implementation of the MUSE-RP protocol client
public class MuseRPClient : IClientProtocol
{
    //references

    protected Client client;
    private MessageHandler customHandler = new MessageHandler();

    #region Constructor
    public MuseRPClient(ConnectionInfo serverInfo, HostOptions options, int timeOutConnection, int connectionTries)
    {

        options.messageHandler = customHandler;
        client = new Client(options, serverInfo, timeOutConnection, connectionTries, true);
    }
    #endregion
    #region Handlers
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        customHandler.AddHandler(type, handler);
    }

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        client.AddOnConnectionFailureHandler(onConnectionFailure);
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        client.AddOnDisconnectedHandler(onDisconnected);
    }
    public void RemoveHandler(ushort type)
    {
        customHandler.RemoveHandler(type);
    }
    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        client.RemoveOnDisconnectedHandler(onDisconnected);
    }

    #endregion
    #region Senders
    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        client.SendToServer(type, reliable, message);
    }
    #endregion
    #region Events

    public void OnAppQuit()
    {
        if (client == null)
        {
            return;
        }
        if (client.IsConnected())
        {
            client.SendEnd();
            client.ReceiveEnd(new MessageObject(), null);

        }

    }
    public void OnStart(OnConnectedDelegate onConnected)
    {
        client.AddOnConnectedHandler(onConnected);
        client.Start();
        client.TryConnect();
        Console.instance.WriteLine("Trying to connect...");
    }

    public void TryConnect()
    {
        client.TryConnect();
    }

    #endregion

}
