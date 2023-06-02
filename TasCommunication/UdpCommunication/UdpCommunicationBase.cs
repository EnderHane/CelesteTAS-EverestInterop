using System.Net;
using System.Net.Sockets;

namespace TasCommunication.UdpCommunication;

public abstract class UdpCommunicationBase : ICommunicationBase {

    public bool IsInitialized => false;

    private UdpClient client;

    protected UdpCommunicationBase(IPEndPoint ip) {
        client = new UdpClient(ip);
    }

}

