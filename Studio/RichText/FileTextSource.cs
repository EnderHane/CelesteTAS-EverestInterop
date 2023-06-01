using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CelesteStudio.RichText;

/// <summary>
/// This class contains the source text (chars and styles).
/// It stores a text lines, the manager of commands, undo/redo stack, styles.
/// </summary>
public class FileTextSource : TextSource, IDisposable {
    readonly List<int> sourceFileLinePositions = new();
    readonly System.Windows.Forms.Timer timer = new();
    private FileStream fs;

    Encoding fileEncoding;
    string path;

    // /// <summary>
    // /// Occurs when need to save line in the file
    // /// </summary>
    // public event EventHandler<LinePushedEventArgs> LinePushed;

    public FileTextSource(StudioTextEdit currentTB)
        : base(currentTB) {
        timer.Interval = 10000;
        timer.Tick += new EventHandler(Timer_Tick);
        timer.Enabled = true;
    }

    private FileStream Fs {
        get {
            int retry = 0;
            while (retry++ < 10) {
                try {
                    return fs ??= new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                } catch (IOException) {
                    Thread.Sleep(50);
                }
            }

            return fs;
        }
    }

    public override Line this[int i] {
        get {
            if (LineList[i] != null) {
                return LineList[i];
            } else {
                for (int j = 0; ; j++) {
                    try {
                        LoadLineFromSourceFile(i);
                        CloseFile();
                        break;
                    } catch (IOException) {
                        if (j > 5) {
                            throw;
                        }

                        Thread.Sleep(5);
                    }
                }
            }

            return LineList[i];
        }
        set => throw new NotImplementedException();
    }

    public override void Dispose() {
        if (Fs != null) {
            Fs.Dispose();
        }

        timer.Dispose();
    }

    /// <summary>
    /// Occurs when need to display line in the textbox
    /// </summary>
    public event EventHandler<LineNeededEventArgs> LineNeeded;

    void Timer_Tick(object sender, EventArgs e) {
        timer.Enabled = false;
        try {
            //UnloadUnusedLines();
        } finally {
            timer.Enabled = true;
        }
    }

    private void UnloadUnusedLines() {
        const int margin = 2000;
        int iStartVisibleLine = CurrentTB.VisibleRange.Start.Line;
        int iFinishVisibleLine = CurrentTB.VisibleRange.End.Line;

        int count = 0;
        for (int i = 0; i < Count; i++) {
            if (LineList[i] != null && !LineList[i].IsChanged && Math.Abs(i - iFinishVisibleLine) > margin) {
                LineList[i] = null;
                count++;
            }
        }
    }

    public void OpenFile(string fileName, Encoding enc) {
        Clear();

        CloseFile();

        path = fileName;
        long length = Fs.Length;
        //read signature
        enc = DefineEncoding(enc, Fs);
        int shift = DefineShift(enc);
        //first line
        sourceFileLinePositions.Add((int) Fs.Position);
        LineList.Add(null);
        //other lines
        while (Fs.Position < length) {
            int b = Fs.ReadByte();
            if (b == 10) // char \n
            {
                sourceFileLinePositions.Add((int) (Fs.Position) + shift);
                LineList.Add(null);
            }
        }

        Line[] temp = new Line[100];
        int c = LineList.Count;
        LineList.AddRange(temp);
        LineList.TrimExcess();
        LineList.RemoveRange(c, temp.Length);


        int[] temp2 = new int[100];
        c = LineList.Count;
        sourceFileLinePositions.AddRange(temp2);
        sourceFileLinePositions.TrimExcess();
        sourceFileLinePositions.RemoveRange(c, temp.Length);


        fileEncoding = enc;

        OnLineInserted(0, Count);
        //load first lines for calc width of the text
        int linesCount = Math.Min(LineList.Count, CurrentTB.Height / CurrentTB.CharHeight);
        for (int i = 0; i < linesCount; i++) {
            LoadLineFromSourceFile(i);
        }

        NeedRecalc(new TextChangedEventArgs(0, 1));
        CloseFile();
    }

    private int DefineShift(Encoding enc) {
        if (enc.IsSingleByte) {
            return 0;
        }

        if (enc.HeaderName == "unicodeFFFE") {
            return 0; //UTF16 BE
        }

        if (enc.HeaderName == "utf-16") {
            return 1; //UTF16 LE
        }

        if (enc.HeaderName == "utf-32BE") {
            return 0; //UTF32 BE
        }

        if (enc.HeaderName == "utf-32") {
            return 3; //UTF32 LE
        }

        return 0;
    }

    private static Encoding DefineEncoding(Encoding enc, FileStream fs) {
        int bytesPerSignature = 0;
        byte[] signature = new byte[4];
        int c = fs.Read(signature, 0, 4);
        if (signature[0] == 0xFF && signature[1] == 0xFE && signature[2] == 0x00 && signature[3] == 0x00 && c >= 4) {
            enc = Encoding.UTF32; //UTF32 LE
            bytesPerSignature = 4;
        } else if (signature[0] == 0x00 && signature[1] == 0x00 && signature[2] == 0xFE && signature[3] == 0xFF) {
            enc = new UTF32Encoding(true, true); //UTF32 BE
            bytesPerSignature = 4;
        } else if (signature[0] == 0xEF && signature[1] == 0xBB && signature[2] == 0xBF) {
            enc = Encoding.UTF8; //UTF8
            bytesPerSignature = 3;
        } else if (signature[0] == 0xFE && signature[1] == 0xFF) {
            enc = Encoding.BigEndianUnicode; //UTF16 BE
            bytesPerSignature = 2;
        } else if (signature[0] == 0xFF && signature[1] == 0xFE) {
            enc = Encoding.Unicode; //UTF16 LE
            bytesPerSignature = 2;
        }

        fs.Seek(bytesPerSignature, SeekOrigin.Begin);

        return enc;
    }

    public void CloseFile() {
        if (fs != null) {
            fs.Dispose();
        }

        fs = null;
    }

    public override void ClearIsChanged() {
        foreach (var line in LineList) {
            if (line != null) {
                line.IsChanged = false;
            }
        }
    }

    private void LoadLineFromSourceFile(int i) {
        var line = CreateLine();
        Fs.Seek(sourceFileLinePositions[i], SeekOrigin.Begin);
        StreamReader sr = new(Fs, fileEncoding);

        string s = sr.ReadLine();
        s ??= "";

        //call event handler
        if (LineNeeded != null) {
            var args = new LineNeededEventArgs(s, i);
            LineNeeded(this, args);
            s = args.DisplayedLineText;
            if (s == null) {
                return;
            }
        }

        foreach (char c in s) {
            line.Add(new StudioChar(c));
        }

        LineList[i] = line;
    }

    public override void InsertLine(int index, Line line) {
        sourceFileLinePositions.Insert(index, -1);
        base.InsertLine(index, line);
    }

    public override void RemoveLine(int index, int count) {
        sourceFileLinePositions.RemoveRange(index, count);
        base.RemoveLine(index, count);
    }

    public override int GetLineLength(int i) {
        if (LineList[i] == null) {
            return 0;
        } else {
            return LineList[i].Count;
        }
    }

    public override bool LineHasFoldingStartMarker(int iLine) {
        if (LineList[iLine] == null) {
            return false;
        } else {
            return !string.IsNullOrEmpty(LineList[iLine].FoldingStartMarker);
        }
    }

    public override bool LineHasFoldingEndMarker(int iLine) {
        if (LineList[iLine] == null) {
            return false;
        } else {
            return !string.IsNullOrEmpty(LineList[iLine].FoldingEndMarker);
        }
    }

    internal void UnloadLine(int iLine) {
        //if (lines[iLine] != null && !lines[iLine].IsChanged)
        //	lines[iLine] = null;
    }
}

public class LineNeededEventArgs : EventArgs {
    public LineNeededEventArgs(string sourceLineText, int displayedLineIndex) {
        SourceLineText = sourceLineText;
        DisplayedLineIndex = displayedLineIndex;
        DisplayedLineText = sourceLineText;
    }

    public string SourceLineText { get; private set; }
    public int DisplayedLineIndex { get; private set; }

    /// <summary>
    /// This text will be displayed in textbox
    /// </summary>
    public string DisplayedLineText { get; set; }
}

public class LinePushedEventArgs : EventArgs {
    public LinePushedEventArgs(string sourceLineText, int displayedLineIndex, string displayedLineText) {
        SourceLineText = sourceLineText;
        DisplayedLineIndex = displayedLineIndex;
        DisplayedLineText = displayedLineText;
        SavedText = displayedLineText;
    }

    public string SourceLineText { get; private set; }
    public int DisplayedLineIndex { get; private set; }

    /// <summary>
    /// This property contains only changed text.
    /// If text of line is not changed, this property contains null.
    /// </summary>
    public string DisplayedLineText { get; private set; }

    /// <summary>
    /// This text will be saved in the file
    /// </summary>
    public string SavedText { get; set; }
}