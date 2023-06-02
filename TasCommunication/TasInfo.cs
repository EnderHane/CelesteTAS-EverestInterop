using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TasCommunication;

// ReSharper disable once StructCanBeMadeReadOnly
public readonly record struct TasInfo {
    [JsonInclude] public readonly int CurrentLine;
    [JsonInclude] public readonly string CurrentLineSuffix;
    [JsonInclude] public readonly int CurrentFrameInTas;
    [JsonInclude] public readonly int TotalFrames;
    [JsonInclude] public readonly int SaveStateLine;
    [JsonInclude] public readonly int TasStates;
    [JsonInclude] public readonly string GameInfo;
    [JsonInclude] public readonly string LevelName;
    [JsonInclude] public readonly string ChapterTime;

    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once ConvertToPrimaryConstructor
    [JsonConstructor]
    public TasInfo(
        int currentLine, 
        string currentLineSuffix, 
        int currentFrameInTas, 
        int totalFrames, 
        int saveStateLine, 
        int tasStates,
        string gameInfo, 
        string levelName, 
        string chapterTime
    ) {
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