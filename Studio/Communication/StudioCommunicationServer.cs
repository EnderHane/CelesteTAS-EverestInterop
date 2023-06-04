using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using StudioCommunication;
using TasCommunication;

namespace CelesteStudio.Communication;

public sealed class StudioCommunicationServer : StudioCommunicationBase, ICommunicationServer {
    private StudioCommunicationServer() { }
    internal static StudioCommunicationServer Instance { get; private set; }

    internal static void Run() {
        //this should be modified to check if there's another studio open as well
        if (Instance != null) {
            return;
        }

        Instance = new StudioCommunicationServer();

        ThreadStart mainLoop = Instance.UpdateLoop;
        Thread updateThread = new(mainLoop) {
            CurrentCulture = CultureInfo.InvariantCulture,
            Name = "StudioCom Server",
            IsBackground = true
        };
        updateThread.Start();
    }

    private readonly Channel<TasInfo> tasInfoChannel = Channel.CreateBounded<TasInfo>(
        new BoundedChannelOptions(1) {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public TasInfo? CurrentTasInfo {
        get {
            return (tasInfoChannel?.Reader.TryPeek(out TasInfo tasInfo) ?? false) ? tasInfo : null;
        }
    }

    private readonly ConcurrentDictionary<long, string> returnDatas = new();

    protected override bool NeedsToWait() {
        return base.NeedsToWait() || Studio.Instance.richText.IsChanged;
    }

    protected override void WriteReset() {
        // ignored
    }

    public void ExternalReset() => WritingChannel.Writer.TryWrite(() => throw new NeedsResetException());

    #region Read

    protected override void ReadData(Message message) {
        switch (message.Id) {
            case MessageID.EstablishConnection:
                throw new NeedsResetException("Recieved initialization message (EstablishConnection) from main loop");
            case MessageID.Reset:
                throw new NeedsResetException("Recieved reset message from main loop");
            case MessageID.Wait:
                ProcessWait();
                break;
            case MessageID.SendState:
                ProcessSendState(message.Data);
                break;
            case MessageID.SendCurrentBindings:
                ProcessSendCurrentBindings(message.Data);
                break;
            case MessageID.UpdateLines:
                ProcessUpdateLines(message.Data);
                break;
            case MessageID.SendPath:
                throw new NeedsResetException("Recieved initialization message (SendPath) from main loop");
            case MessageID.ReturnData:
                ProcessReturnData(message.Data);
                break;
            default:
                throw new InvalidOperationException($"{message.Id}");
        }
    }

    private void ProcessSendState(byte[] data) {
        TasInfo studioInfo = TasInfo.FromUtf8JsonBytes(data);
        tasInfoChannel.Writer.TryWrite(studioInfo);
    }

    private void ProcessSendCurrentBindings(byte[] data) {
        Dictionary<int, List<int>> nativeBindings = SerializationUtil.DeserializeUtf8JsonBytes<Dictionary<int, List<int>>>(data);
        Dictionary<HotkeyID, List<Keys>> bindings =
            nativeBindings.ToDictionary(pair => (HotkeyID) pair.Key, pair => pair.Value.Cast<Keys>().ToList());
        foreach (var pair in bindings) {
            Log(pair.ToString());
        }

        CommunicationUtil.SetBindings(bindings);
    }

    private void ProcessVersionInfo(byte[] data) {
        string[] versionInfos = SerializationUtil.DeserializeUtf8JsonBytes<string[]>(data);
        string modVersion = ErrorLog.ModVersion = versionInfos[0];
        string minStudioVersion = versionInfos[1];

        if (new Version(minStudioVersion + ".0") > Studio.Version) {
            MessageBox.Show(
                $"CelesteTAS v{modVersion} require CelesteStudio v {minStudioVersion} at least. Please manually extract CelesteStudio from the \"game_path\\Mods\\CelesteTAS.zip\" file.",
                "Communication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void ProcessUpdateLines(byte[] data) {
        Dictionary<int, string> updateLines = SerializationUtil.DeserializeUtf8JsonBytes<Dictionary<int, string>>(data);
        CommunicationUtil.UpdateLines(updateLines);
    }

    private void ProcessReturnData(byte[] data) {
        (long token, string gameData) = SerializationUtil.DeserializeUtf8JsonBytes<(long, string)>(data);
        returnDatas[token] = gameData;
    }

    #endregion

    #region Write

    protected override void EstablishConnection() {
        var studio = this;
        // var celeste = this;

        Message lastMessage;

        studio.ReadMessage();
        studio.WriteMessageGuaranteed(new Message(MessageID.EstablishConnection, new byte[0]));
        // celeste.ReadMessageGuaranteed();

        studio.SendPathNow(Studio.Instance.richText.CurrentFileName, false);
        // lastMessage = celeste.ReadMessageGuaranteed();

        // celeste.SendCurrentBindings(Hotkeys.listHotkeyKeys);
        lastMessage = studio.ReadMessageGuaranteed();
        if (lastMessage.Id != MessageID.SendCurrentBindings) {
            throw new NeedsResetException("Invalid data recieved while establishing connection");
        }

        studio.ProcessSendCurrentBindings(lastMessage.Data);

        // celeste.SendModVersion();
        lastMessage = studio.ReadMessageGuaranteed();
        if (lastMessage.Id != MessageID.VersionInfo) {
            throw new NeedsResetException("Invalid data recieved while establishing connection");
        }

        studio.ProcessVersionInfo(lastMessage.Data);

        IsInitialized = true;
    }

    public void SendPath(string path) => WritingChannel.Writer.TryWrite(() => SendPathNow(path, false));
    public void ConvertToLibTas(string path) => WritingChannel.Writer.TryWrite(() => ConvertToLibTasNow(path));
    public void SendHotkeyPressed(HotkeyID hotkey, bool released = false) => WritingChannel.Writer.TryWrite(() => SendHotkeyPressedNow(hotkey, released));
    public void ToggleGameSetting(string settingName, object value) => WritingChannel.Writer.TryWrite(() => ToggleGameSettingNow(settingName, value));

    public Task<string> GetDataFromGameAsync(GameDataType gameDataType, object arg, CancellationToken ct) {
        long token = DateTime.Now.Ticks;
        WritingChannel.Writer.TryWrite(() => GetGameDataNow(gameDataType, token, arg));
        return Task.Run(() => {
            while (!ct.IsCancellationRequested) {
                if (SpinWait.SpinUntil(() => returnDatas.ContainsKey(token), 10) && returnDatas.TryRemove(token, out string data)) { 
                    return data; 
                }
            }
            return null;
        }, ct);
    }

    private void SendPathNow(string path, bool canFail) {
        if (IsInitialized || !canFail) {
            byte[] pathBytes = path != null ? Encoding.UTF8.GetBytes(path) : new byte[0];

            WriteMessageGuaranteed(new Message(MessageID.SendPath, pathBytes));
        }
    }

    private void ConvertToLibTasNow(string path) {
        if (!IsInitialized) {
            return;
        }

        byte[] pathBytes = string.IsNullOrEmpty(path) ? new byte[0] : Encoding.UTF8.GetBytes(path);

        WriteMessageGuaranteed(new Message(MessageID.ConvertToLibTas, pathBytes));
    }

    private void SendHotkeyPressedNow(HotkeyID hotkey, bool released) {
        if (!IsInitialized) {
            return;
        }

        byte[] hotkeyBytes = { (byte) hotkey, Convert.ToByte(released) };
        WriteMessageGuaranteed(new Message(MessageID.SendHotkeyPressed, hotkeyBytes));
    }

    private void ToggleGameSettingNow(string settingName, object value) {
        if (!IsInitialized) {
            return;
        }

        byte[] bytes = SerializationUtil.SerializeToUtf8JsonBytes((settingName, value));
        WriteMessageGuaranteed(new Message(MessageID.ToggleGameSetting, bytes));
    }

    private void GetGameDataNow(GameDataType gameDataType, long communicationToken, object arg) {
        if (!IsInitialized) {
            return;
        }

        byte[] bytes = SerializationUtil.SerializeToUtf8JsonBytes(((byte) gameDataType, communicationToken, arg));
        WriteMessageGuaranteed(new Message(MessageID.GetData, bytes));
    }

    #endregion
}