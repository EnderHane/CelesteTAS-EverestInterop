using System.Threading;
using System.Threading.Tasks;
using StudioCommunication;
using TasCommunication;

namespace CelesteStudio.Communication;
public static class CommunicationServer {

    public static ICommunicationServer Instance { get => StudioCommunicationServer.Instance; }

    public static void Run() {
        StudioCommunicationServer.Run();
    }

}

public interface ICommunicationServer : ICommunicationBase {

    TasInfo? CurrentTasInfo { get; }

    void SendPath(string path);

    void ConvertToLibTas(string path);

    void SendHotkeyPressed(HotkeyID hotkey, bool released = false);

    void ToggleGameSetting(string settingName, object value);

    Task<string> GetDataFromGameAsync(GameDataType gameDataType, object arg, CancellationToken ct);

    void ExternalReset();

    void WriteWait();

}

