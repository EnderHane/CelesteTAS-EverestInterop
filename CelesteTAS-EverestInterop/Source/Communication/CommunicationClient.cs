using System.Collections.Generic;
using StudioCommunication;
using TasCommunication;

namespace TAS.Communication;

public static class CommunicationClient {

    public static ICommunicationClient Instance { get => StudioCommunicationClient.Instance; }

    public static void Run() {
        StudioCommunicationClient.Run();
    }
}

public interface ICommunicationClient : ICommunicationBase {

    void SendCurrentBindings(bool forceSend = false);

    void SendState(StudioInfo studioInfo, bool canFail);

    void UpdateLines(Dictionary<int, string> lines);

}

