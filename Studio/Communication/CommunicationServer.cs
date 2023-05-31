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

    void SendPath(string path);

    void ConvertToLibTas(string path);

    void SendHotkeyPressed(HotkeyID hotkey, bool released = false);

    void ToggleGameSetting(string settingName, object value);

    void GetDataFromGame(GameDataType gameDataType, object arg);

    void ExternalReset();

    void WriteWait();

}

