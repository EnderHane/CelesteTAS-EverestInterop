using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TasCommunication;

// ReSharper disable once StructCanBeMadeReadOnly
public readonly record struct TasInfo {
    public readonly int CurrentLine;
    public readonly string CurrentLineSuffix;
    public readonly int CurrentFrameInTas;
    public readonly int TotalFrames;
    public readonly int SaveStateLine;
    public readonly int TasStates;
    public readonly string GameInfo;
    public readonly string LevelName;
    public readonly string ChapterTime;

    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once ConvertToPrimaryConstructor
    public TasInfo(
        int currentLine, string currentLineSuffix, int currentFrameInTas, int totalFrames, int saveStateLine, int tasStates,
        string gameInfo, string levelName, string chapterTime) {
        CurrentLine = currentLine;
        CurrentLineSuffix = currentLineSuffix;
        CurrentFrameInTas = currentFrameInTas;
        TotalFrames = totalFrames;
        SaveStateLine = saveStateLine;
        TasStates = tasStates;
        GameInfo = gameInfo;
        LevelName = levelName;
        ChapterTime = chapterTime;
    }

    public byte[] ToUtf8JsonBytes() => SerializationUtil.SerializeToUtf8JsonBytes(this);

    public static TasInfo FromUtf8JsonBytes(byte[] bytes) => SerializationUtil.DeserializeUtf8JsonBytes<TasInfo>(bytes);

}