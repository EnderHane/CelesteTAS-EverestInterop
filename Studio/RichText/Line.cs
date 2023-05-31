using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CelesteStudio.RichText;

public class Line : IList<StudioChar> {
    protected List<StudioChar> Chars;

    internal Line(int uid) {
        UniqueId = uid;
        Chars = new List<StudioChar>();
    }

    public string FoldingStartMarker { get; set; }
    public string FoldingEndMarker { get; set; }
    public bool IsChanged { get; set; }

    /// <summary>
    /// Time of last visit of caret in this line
    /// </summary>
    /// <remarks>This property can be used for forward/backward navigating</remarks>
    public DateTime LastVisit { get; set; }

    public Brush BackgroundBrush { get; set; }
    public int UniqueId { get; private set; }
    public int AutoIndentSpacesNeededCount { get; internal set; }

    public virtual string Text {
        get {
            StringBuilder sb = new(Count);
            foreach (StudioChar c in this) {
                sb.Append(c.Char_);
            }

            return sb.ToString();
        }
    }

    public int StartSpacesCount {
        get {
            int spacesCount = 0;
            for (int i = 0; i < Count; i++) {
                if (this[i].Char_ == ' ') {
                    spacesCount++;
                } else {
                    break;
                }
            }

            return spacesCount;
        }
    }

    public int IndexOf(StudioChar item) {
        return Chars.IndexOf(item);
    }

    public void Insert(int index, StudioChar item) {
        Chars.Insert(index, item);
    }

    public void RemoveAt(int index) {
        Chars.RemoveAt(index);
    }

    public StudioChar this[int index] {
        get => Chars[index];
        set => Chars[index] = value;
    }

    public void Add(StudioChar item) {
        Chars.Add(item);
    }

    public void Clear() {
        Chars.Clear();
    }

    public bool Contains(StudioChar item) {
        return Chars.Contains(item);
    }

    public void CopyTo(StudioChar[] array, int arrayIndex) {
        Chars.CopyTo(array, arrayIndex);
    }

    public int Count => Chars.Count;

    public bool IsReadOnly => false;

    public bool Remove(StudioChar item) {
        return Chars.Remove(item);
    }

    public IEnumerator<StudioChar> GetEnumerator() {
        return Chars.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return Chars.GetEnumerator() as System.Collections.IEnumerator;
    }

    /// <summary>
    /// Clears style of chars, delete folding markers
    /// </summary>
    public void ClearStyle(StyleIndex styleIndex) {
        FoldingStartMarker = null;
        FoldingEndMarker = null;
        for (int i = 0; i < Count; i++) {
            StudioChar c = this[i];
            c.Style &= ~styleIndex;
            this[i] = c;
        }
    }

    public void ClearFoldingMarkers() {
        FoldingStartMarker = null;
        FoldingEndMarker = null;
    }

    public virtual void RemoveRange(int index, int count) {
        if (index >= Count) {
            return;
        }

        Chars.RemoveRange(index, Math.Min(Count - index, count));
    }

    public virtual void TrimExcess() {
        Chars.TrimExcess();
    }

    public virtual void AddRange(IEnumerable<StudioChar> collection) {
        Chars.AddRange(collection);
    }
}

public struct LineInfo {
    List<int> cutOffPositions;

    //Y coordinate of line on screen
    internal int StartY;
    public VisibleState VisibleState;

    public LineInfo(int startY) {
        cutOffPositions = null;
        VisibleState = VisibleState.Visible;
        this.StartY = startY;
    }

    public List<int> CutOffPositions {
        get {
            if (cutOffPositions == null) {
                cutOffPositions = new List<int>();
            }

            return cutOffPositions;
        }
    }

    public int WordWrapStringsCount {
        get {
            switch (VisibleState) {
                case VisibleState.Visible:
                    if (cutOffPositions == null) {
                        return 1;
                    } else {
                        return cutOffPositions.Count + 1;
                    }
                case VisibleState.Hidden:
                    return 0;
                case VisibleState.StartOfHiddenBlock:
                    return 1;
            }

            return 0;
        }
    }

    internal int GetWordWrapStringStartPosition(int iWordWrapLine) {
        return iWordWrapLine == 0 ? 0 : CutOffPositions[iWordWrapLine - 1];
    }

    internal int GetWordWrapStringFinishPosition(int iWordWrapLine, Line line) {
        if (WordWrapStringsCount <= 0) {
            return 0;
        }

        return iWordWrapLine == WordWrapStringsCount - 1 ? line.Count - 1 : CutOffPositions[iWordWrapLine] - 1;
    }

    public int GetWordWrapStringIndex(int iChar) {
        if (cutOffPositions == null || cutOffPositions.Count == 0) {
            return 0;
        }

        for (int i = 0; i < cutOffPositions.Count; i++) {
            if (cutOffPositions[i] > /*>=*/ iChar) {
                return i;
            }
        }

        return cutOffPositions.Count;
    }

    internal void CalcCutOffs(int maxCharsPerLine, bool allowIME, bool charWrap, Line line) {
        int segmentLength = 0;
        int cutOff = 0;
        CutOffPositions.Clear();

        for (int i = 0; i < line.Count; i++) {
            char c = line[i].Char_;
            if (charWrap) {
                //char wrapping
                cutOff = Math.Min(i + 1, line.Count - 1);
            } else {
                //word wrapping
                if (allowIME && IsCJKLetter(c)) //in CJK languages cutoff can be in any letter
                {
                    cutOff = i;
                } else if (!char.IsLetterOrDigit(c) && c != '_') {
                    cutOff = Math.Min(i + 1, line.Count - 1);
                }
            }

            segmentLength++;

            if (segmentLength == maxCharsPerLine) {
                if (cutOff == 0 || (cutOffPositions.Count > 0 && cutOff == cutOffPositions[cutOffPositions.Count - 1])) {
                    cutOff = i + 1;
                }

                CutOffPositions.Add(cutOff);
                segmentLength = 1 + i - cutOff;
            }
        }
    }

    private bool IsCJKLetter(char c) {
        int code = Convert.ToInt32(c);
        return
            (code is >= 0x3300 and <= 0x33FF) ||
            (code is >= 0xFE30 and <= 0xFE4F) ||
            (code is >= 0xF900 and <= 0xFAFF) ||
            (code is >= 0x2E80 and <= 0x2EFF) ||
            (code is >= 0x31C0 and <= 0x31EF) ||
            (code is >= 0x4E00 and <= 0x9FFF) ||
            (code is >= 0x3400 and <= 0x4DBF) ||
            (code is >= 0x3200 and <= 0x32FF) ||
            (code is >= 0x2460 and <= 0x24FF) ||
            (code is >= 0x3040 and <= 0x309F) ||
            (code is >= 0x2F00 and <= 0x2FDF) ||
            (code is >= 0x31A0 and <= 0x31BF) ||
            (code is >= 0x4DC0 and <= 0x4DFF) ||
            (code is >= 0x3100 and <= 0x312F) ||
            (code is >= 0x30A0 and <= 0x30FF) ||
            (code is >= 0x31F0 and <= 0x31FF) ||
            (code is >= 0x2FF0 and <= 0x2FFF) ||
            (code is >= 0x1100 and <= 0x11FF) ||
            (code is >= 0xA960 and <= 0xA97F) ||
            (code is >= 0xD7B0 and <= 0xD7FF) ||
            (code is >= 0x3130 and <= 0x318F) ||
            (code is >= 0xAC00 and <= 0xD7AF);
    }
}

public enum VisibleState : byte {
    Visible,
    StartOfHiddenBlock,
    Hidden
}

public enum IndentMarker {
    None,
    Increased,
    Decreased
}