using System.Collections.Generic;
using TasCommunication;

namespace TAS.Communication;

public static class CommunicationClient {

    public static ICommunicationClient Instance { get => StudioCommunicationClient.Instance; }

    public static void Run() {
        StudioCommunicationClient.Run();
    }
}

public interface ICommunicationClient : ICommunicationBase {

    void SetStudioInteractBindings<T>(IDictionary<HotkeyID, T> bindings) where T : IList<int>;

    void SendCurrentBindings(bool forceSend = false);

    void SendState(TasInfo studioInfo, bool canFail);

    void UpdateLines(Dictionary<int, string> lines);

}

