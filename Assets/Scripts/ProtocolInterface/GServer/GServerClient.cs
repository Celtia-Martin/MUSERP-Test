using System;
using System.Net;
using System.Text;
using System.Threading;
using GServer;
using GServer.Messages;
using Muse_RP.Message;

public class GServerClient : IClientProtocol
{
    private int port = 7777;
    private Host clientHost;
    private IPEndPoint serverEndpoint;
    public GServerClient(int serverPort, string ipAddress)
    {
        IPAddress address = IPAddress.Parse(ipAddress);
        serverEndpoint = new IPEndPoint(address, serverPort);
        clientHost = new Host(port);
    }

    public void AddHandler(ushort type, MessageDelegate handler)
    {
        clientHost.AddHandler((short)type, (m, s) =>
        {
          
            MessageObject messageObject = new MessageObject(type,0,0,false,false,false,false,m.Body);
            Muse_RP.Hosts.Connection conn = new Muse_RP.Hosts.Connection(s.EndPoint, true);
            handler?.Invoke(messageObject, conn);

        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {

        clientHost.AddHandler((short)type, (m, s) =>
        {
            handler?.Invoke(m.Body);

        });
    }

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        clientHost.OnException += (e) => onConnectionFailure?.Invoke();
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        clientHost.OnException += (e) => onDisconnected?.Invoke();
    }

    public void OnAppQuit()
    {
        clientHost.Dispose();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        clientHost.StartListen();
        clientHost.OnConnect += ()=>onConnected?.Invoke();
        Timer timer = new Timer(o => clientHost.Tick());
        timer.Change(10, 10);
        TryConnect();
        
    }

    public void RemoveHandler(ushort type)
    {
        //not implemented
    }

    public void RemoveOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        //not implemented
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        //not implemented
    }

    public void SendToServer(ushort type, byte[] data, bool reliable = true)
    {
        Message message = new Message((short)type, reliable? Mode.Reliable: Mode.Ordered, data);
        clientHost.Send(message);
 
    }

    public void TryConnect()
    {
        clientHost.BeginConnect(serverEndpoint);
    }


}


