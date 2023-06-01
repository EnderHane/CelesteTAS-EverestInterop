using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CelesteStudio.RichText;

/// <summary>
/// Diapason of text chars
/// </summary>
public class Range : IEnumerable<Place> {
    public readonly StudioTextEdit Tb;
    private List<Place> cachedCharIndexToPlace;

    private string cachedText;
    private int cachedTextVersion = -1;

    private bool columnSelectionMode;
    private Place end;
    private int preferedPos = -1;
    private Place start;
    private int updating = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public Range(StudioTextEdit tb) {
        Tb = tb;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Range(StudioTextEdit tb, int startChar, int startLine, int endChar, int endLine)
        : this(tb) {
        start = new Place(startChar, startLine);
        end = new Place(endChar, endLine);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Range(StudioTextEdit tb, Place start, Place end)
        : this(tb) {
        this.start = start;
        this.end = end;
    }

    /// <summary>
    /// Return true if no selected text
    /// </summary>
    public virtual bool IsEmpty {
        get {
            if (ColumnSelectionMode) {
                return Start.Char == End.Char;
            } else {
                return Start == End;
            }
        }
    }

    /// <summary>
    /// Column selection mode
    /// </summary>
    public bool ColumnSelectionMode {
        get => columnSelectionMode;
        set => columnSelectionMode = value;
    }

    /// <summary>
    /// Start line and char position
    /// </summary>
    public Place Start {
        get => start;
        set {
            end = start = value;
            preferedPos = -1;
            OnSelectionChanged();
        }
    }

    /// <summary>
    /// Finish line and char position
    /// </summary>
    public Place End {
        get => end;
        set {
            end = value;
            OnSelectionChanged();
        }
    }

    /// <summary>
    /// Text of range
    /// </summary>
    /// <remarks>This property has not 'set' accessor because undo/redo stack works only with 
    /// FastColoredTextBox.Selection range. So, if you want to set text, you need to use FastColoredTextBox.Selection
    /// and FastColoredTextBox.InsertText() mehtod.
    /// </remarks>
    public virtual string Text {
        get {
            if (ColumnSelectionMode) {
                return Text_ColumnSelectionMode;
            }

            int fromLine = Math.Min(end.Line, start.Line);
            int toLine = Math.Max(end.Line, start.Line);
            int fromChar = FromX;
            int toChar = ToX;
            if (fromLine < 0) {
                return null;
            }

            //
            StringBuilder sb = new();
            for (int y = fromLine; y <= toLine; y++) {
                int fromX = y == fromLine ? fromChar : 0;
                int toX = y == toLine ? Math.Min(Tb[y].Count - 1, toChar - 1) : Tb[y].Count - 1;
                for (int x = fromX; x <= toX; x++) {
                    sb.Append(Tb[y][x].Char_);
                }

                if (y != toLine && fromLine != toLine) {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Returns first char after Start place
    /// </summary>
    public char CharAfterStart {
        get {
            if (Start.Char >= Tb[Start.Line].Count) {
                return '\n';
            } else {
                return Tb[Start.Line][Start.Char].Char_;
            }
        }
    }

    /// <summary>
    /// Returns first char before Start place
    /// </summary>
    public char CharBeforeStart {
        get {
            if (Start.Char > Tb[Start.Line].Count) {
                return '\n';
            }

            if (Start.Char <= 0) {
                return '\n';
            } else {
                return Tb[Start.Line][Start.Char - 1].Char_;
            }
        }
    }

    /// <summary>
    /// Return minimum of end.X and start.X
    /// </summary>
    internal int FromX {
        get {
            if (end.Line < start.Line) {
                return end.Char;
            }

            if (end.Line > start.Line) {
                return start.Char;
            }

            return Math.Min(end.Char, start.Char);
        }
    }

    /// <summary>
    /// Return maximum of end.X and start.X
    /// </summary>
    internal int ToX {
        get {
            if (end.Line < start.Line) {
                return start.Char;
            }

            if (end.Line > start.Line) {
                return end.Char;
            }

            return Math.Max(end.Char, start.Char);
        }
    }

    public RangeRect Bounds {
        get {
            int minX = Math.Min(Start.Char, End.Char);
            int minY = Math.Min(Start.Line, End.Line);
            int maxX = Math.Max(Start.Char, End.Char);
            int maxY = Math.Max(Start.Line, End.Line);
            return new RangeRect(minY, minX, maxY, maxX);
        }
    }

    IEnumerator<Place> IEnumerable<Place>.GetEnumerator() {
        if (ColumnSelectionMode) {
            foreach (var p in GetEnumerator_ColumnSelectionMode()) {
                yield return p;
            }

            yield break;
        }

        int fromLine = Math.Min(end.Line, start.Line);
        int toLine = Math.Max(end.Line, start.Line);
        int fromChar = FromX;
        int toChar = ToX;
        if (fromLine < 0) {
            yield break;
        }

        //
        for (int y = fromLine; y <= toLine; y++) {
            int fromX = y == fromLine ? fromChar : 0;
            int toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
            for (int x = fromX; x <= toX; x++) {
                yield return new Place(x, y);
            }
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return (this as IEnumerable<Place>).GetEnumerator();
    }

    public bool Contains(Place place) {
        if (place.Line < Math.Min(start.Line, end.Line)) {
            return false;
        }

        if (place.Line > Math.Max(start.Line, end.Line)) {
            return false;
        }

        Place s = start;
        Place e = end;

        if (s.Line > e.Line || (s.Line == e.Line && s.Char > e.Char)) {
            var temp = s;
            s = e;
            e = temp;
        }

        if (place.Line == s.Line && place.Char < s.Char) {
            return false;
        }

        if (place.Line == e.Line && place.Char > e.Char) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns intersection with other range,
    /// empty range returned otherwise
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public virtual Range GetIntersectionWith(Range range) {
        if (ColumnSelectionMode) {
            return GetIntersectionWith_ColumnSelectionMode(range);
        }

        Range r1 = Clone();
        Range r2 = range.Clone();
        r1.Normalize();
        r2.Normalize();
        Place newStart = r1.Start > r2.Start ? r1.Start : r2.Start;
        Place newEnd = r1.End < r2.End ? r1.End : r2.End;
        if (newEnd < newStart) {
            return new Range(Tb, start, start);
        }

        return Tb.GetRange(newStart, newEnd);
    }

    /// <summary>
    /// Returns union with other range.
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public Range GetUnionWith(Range range) {
        Range r1 = Clone();
        Range r2 = range.Clone();
        r1.Normalize();
        r2.Normalize();
        Place newStart = r1.Start < r2.Start ? r1.Start : r2.Start;
        Place newEnd = r1.End > r2.End ? r1.End : r2.End;

        return Tb.GetRange(newStart, newEnd);
    }

    /// <summary>
    /// Select all chars of control
    /// </summary>
    public void SelectAll() {
        ColumnSelectionMode = false;

        if (Tb.LinesCount == 0) {
            Start = new Place(0, 0);
        } else {
            end = new Place(0, 0);
            start = new Place(Tb[Tb.LinesCount - 1].Count, Tb.LinesCount - 1);
        }

        if (this == Tb.Selection) {
            Tb.Invalidate();
        }
    }

    public void SelectBlock() {
        ColumnSelectionMode = false;

        if (Tb.LinesCount == 0) {
            Start = new Place(0, 0);
        } else {
            Normalize();
            int startLine = start.Line;
            int endLine = end.Line;

            while (startLine > 0) {
                if (Tb.Lines[startLine].Trim().Length == 0) {
                    break;
                }

                startLine--;
            }

            if (startLine > 0 && startLine < Tb.LinesCount - 1) {
                startLine++;
            }

            while (endLine < Tb.LinesCount - 1) {
                if (Tb.Lines[endLine].Trim().Length == 0) {
                    break;
                }

                endLine++;
            }

            start = new Place(0, startLine);
            end = new Place(Tb[endLine].Count, endLine);
        }

        if (this == Tb.Selection) {
            Tb.Invalidate();
        }
    }

    internal void GetText(out string text, out List<Place> charIndexToPlace) {
        //try get cached text
        if (Tb.TextVersion == cachedTextVersion) {
            text = cachedText;
            charIndexToPlace = cachedCharIndexToPlace;
            return;
        }

        //
        int fromLine = Math.Min(end.Line, start.Line);
        int toLine = Math.Max(end.Line, start.Line);
        int fromChar = FromX;
        int toChar = ToX;

        StringBuilder sb = new((toLine - fromLine) * 50);
        charIndexToPlace = new List<Place>(sb.Capacity);
        if (fromLine >= 0) {
            for (int y = fromLine; y <= toLine; y++) {
                int fromX = y == fromLine ? fromChar : 0;
                int toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
                for (int x = fromX; x <= toX; x++) {
                    sb.Append(Tb[y][x].Char_);
                    charIndexToPlace.Add(new Place(x, y));
                }

                if (y != toLine && fromLine != toLine) {
                    foreach (char c in Environment.NewLine) {
                        sb.Append(c);
                        charIndexToPlace.Add(new Place(Tb[y].Count /*???*/, y));
                    }
                }
            }
        }

        text = sb.ToString();
        charIndexToPlace.Add(End > Start ? End : Start);
        //caching
        cachedText = text;
        cachedCharIndexToPlace = charIndexToPlace;
        cachedTextVersion = Tb.TextVersion;
    }

    /// <summary>
    /// Clone range
    /// </summary>
    /// <returns></returns>
    public Range Clone() {
        return (Range) MemberwiseClone();
    }

    /// <summary>
    /// Move range right
    /// </summary>
    /// <remarks>This method jump over folded blocks</remarks>
    public bool GoRight() {
        Place prevStart = start;
        GoRight(false);
        return prevStart != start;
    }

    /// <summary>
    /// Move range left
    /// </summary>
    /// <remarks>This method can to go inside folded blocks</remarks>
    public virtual bool GoRightThroughFolded() {
        if (ColumnSelectionMode) {
            return GoRightThroughFolded_ColumnSelectionMode();
        }

        if (start.Line >= Tb.LinesCount - 1 && start.Char >= Tb[Tb.LinesCount - 1].Count) {
            return false;
        }

        if (start.Char < Tb[start.Line].Count) {
            start.Offset(1, 0);
        } else {
            start = new Place(0, start.Line + 1);
        }

        preferedPos = -1;
        end = start;
        OnSelectionChanged();
        return true;
    }

    /// <summary>
    /// Move range left
    /// </summary>
    /// <remarks>This method jump over folded blocks</remarks>
    public bool GoLeft() {
        ColumnSelectionMode = false;

        Place prevStart = start;
        GoLeft(false);
        return prevStart != start;
    }

    /// <summary>
    /// Move range left
    /// </summary>
    /// <remarks>This method can to go inside folded blocks</remarks>
    public bool GoLeftThroughFolded() {
        ColumnSelectionMode = false;

        if (start.Char == 0 && start.Line == 0) {
            return false;
        }

        if (start.Char > 0) {
            start.Offset(-1, 0);
        } else {
            start = new Place(Tb[start.Line - 1].Count, start.Line - 1);
        }

        preferedPos = -1;
        end = start;
        OnSelectionChanged();
        return true;
    }

    public void GoLeft(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start > end) {
                Start = End;
                return;
            }
        }

        if (start.Char != 0 || start.Line != 0) {
            if (start.Char > 0 && Tb.LineInfos[start.Line].VisibleState == VisibleState.Visible) {
                start.Offset(-1, 0);
            } else {
                int i = Tb.FindPrevVisibleLine(start.Line);
                if (i == start.Line) {
                    return;
                }

                start = new Place(Tb[i].Count, i);
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();

        preferedPos = -1;
    }

    public void GoRight(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start < end) {
                Start = End;
                return;
            }
        }

        if (start.Line < Tb.LinesCount - 1 || start.Char < Tb[Tb.LinesCount - 1].Count) {
            if (start.Char < Tb[start.Line].Count && Tb.LineInfos[start.Line].VisibleState == VisibleState.Visible) {
                start.Offset(1, 0);
            } else {
                int i = Tb.FindNextVisibleLine(start.Line);
                if (i == start.Line) {
                    return;
                }

                start = new Place(0, i);
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();

        preferedPos = -1;
    }

    internal void GoUp(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start.Line > end.Line) {
                Start = End;
                return;
            }
        }

        if (preferedPos < 0) {
            preferedPos = start.Char - Tb.LineInfos[start.Line]
                .GetWordWrapStringStartPosition(Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char));
        }

        int iWW = Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char);
        if (iWW == 0) {
            if (start.Line <= 0) {
                return;
            }

            int i = Tb.FindPrevVisibleLine(start.Line);
            if (i == start.Line) {
                return;
            }

            start.Line = i;
            iWW = Tb.LineInfos[start.Line].WordWrapStringsCount;
        }

        if (iWW > 0) {
            int finish = Tb.LineInfos[start.Line].GetWordWrapStringFinishPosition(iWW - 1, Tb[start.Line]);
            start.Char = Tb.LineInfos[start.Line].GetWordWrapStringStartPosition(iWW - 1) + preferedPos;
            if (start.Char > finish + 1) {
                start.Char = finish + 1;
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    internal void GoPageUp(bool shift) {
        ColumnSelectionMode = false;

        if (preferedPos < 0) {
            preferedPos = start.Char - Tb.LineInfos[start.Line]
                .GetWordWrapStringStartPosition(Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char));
        }

        int pageHeight = Tb.ClientRectangle.Height / Tb.CharHeight - 1;

        for (int i = 0; i < pageHeight; i++) {
            int iWW = Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char);
            if (iWW == 0) {
                if (start.Line <= 0) {
                    break;
                }

                //pass hidden
                int newLine = Tb.FindPrevVisibleLine(start.Line);
                if (newLine == start.Line) {
                    break;
                }

                start.Line = newLine;
                iWW = Tb.LineInfos[start.Line].WordWrapStringsCount;
            }

            if (iWW > 0) {
                int finish = Tb.LineInfos[start.Line].GetWordWrapStringFinishPosition(iWW - 1, Tb[start.Line]);
                start.Char = Tb.LineInfos[start.Line].GetWordWrapStringStartPosition(iWW - 1) + preferedPos;
                if (start.Char > finish + 1) {
                    start.Char = finish + 1;
                }
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    internal void GoDown(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start.Line < end.Line) {
                Start = End;
                return;
            }
        }

        if (preferedPos < 0) {
            preferedPos = start.Char - Tb.LineInfos[start.Line]
                .GetWordWrapStringStartPosition(Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char));
        }

        int iWW = Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char);
        if (iWW >= Tb.LineInfos[start.Line].WordWrapStringsCount - 1) {
            if (start.Line >= Tb.LinesCount - 1) {
                return;
            }

            //pass hidden
            int i = Tb.FindNextVisibleLine(start.Line);
            if (i == start.Line) {
                return;
            }

            start.Line = i;
            iWW = -1;
        }

        if (iWW < Tb.LineInfos[start.Line].WordWrapStringsCount - 1) {
            int finish = Tb.LineInfos[start.Line].GetWordWrapStringFinishPosition(iWW + 1, Tb[start.Line]);
            start.Char = Tb.LineInfos[start.Line].GetWordWrapStringStartPosition(iWW + 1) + preferedPos;
            if (start.Char > finish + 1) {
                start.Char = finish + 1;
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    internal void GoPageDown(bool shift) {
        ColumnSelectionMode = false;

        if (preferedPos < 0) {
            preferedPos = start.Char - Tb.LineInfos[start.Line]
                .GetWordWrapStringStartPosition(Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char));
        }

        int pageHeight = Tb.ClientRectangle.Height / Tb.CharHeight - 1;

        for (int i = 0; i < pageHeight; i++) {
            int iWW = Tb.LineInfos[start.Line].GetWordWrapStringIndex(start.Char);
            if (iWW >= Tb.LineInfos[start.Line].WordWrapStringsCount - 1) {
                if (start.Line >= Tb.LinesCount - 1) {
                    break;
                }

                //pass hidden
                int newLine = Tb.FindNextVisibleLine(start.Line);
                if (newLine == start.Line) {
                    break;
                }

                start.Line = newLine;
                iWW = -1;
            }

            if (iWW < Tb.LineInfos[start.Line].WordWrapStringsCount - 1) {
                int finish = Tb.LineInfos[start.Line].GetWordWrapStringFinishPosition(iWW + 1, Tb[start.Line]);
                start.Char = Tb.LineInfos[start.Line].GetWordWrapStringStartPosition(iWW + 1) + preferedPos;
                if (start.Char > finish + 1) {
                    start.Char = finish + 1;
                }
            }
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    internal void GoHome(bool shift) {
        ColumnSelectionMode = false;

        if (start.Line < 0) {
            return;
        }

        if (Tb.LineInfos[start.Line].VisibleState != VisibleState.Visible) {
            return;
        }

        start = new Place(0, start.Line);

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();

        preferedPos = -1;
    }

    internal void GoEnd(bool shift) {
        ColumnSelectionMode = false;

        if (start.Line < 0) {
            return;
        }

        if (Tb.LineInfos[start.Line].VisibleState != VisibleState.Visible) {
            return;
        }

        start = new Place(Tb[start.Line].Count, start.Line);

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();

        preferedPos = -1;
    }

    /// <summary>
    /// Set style for range
    /// </summary>
    public void SetStyle(Style style) {
        //search code for style
        int code = Tb.GetOrSetStyleLayerIndex(style);
        //set code to chars
        SetStyle(ToStyleIndex(code));
        //
        Tb.Invalidate();
    }

    /// <summary>
    /// Set style for given regex pattern
    /// </summary>
    public void SetStyle(Style style, string regexPattern) {
        //search code for style
        StyleIndex layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
        SetStyle(layer, regexPattern, RegexOptions.None);
    }

    /// <summary>
    /// Set style for given regex
    /// </summary>
    public void SetStyle(Style style, Regex regex) {
        //search code for style
        StyleIndex layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
        SetStyle(layer, regex);
    }


    /// <summary>
    /// Set style for given regex pattern
    /// </summary>
    public void SetStyle(Style style, string regexPattern, RegexOptions options) {
        //search code for style
        StyleIndex layer = ToStyleIndex(Tb.GetOrSetStyleLayerIndex(style));
        SetStyle(layer, regexPattern, options);
    }

    /// <summary>
    /// Set style for given regex pattern
    /// </summary>
    public void SetStyle(StyleIndex styleLayer, string regexPattern, RegexOptions options) {
        if (Math.Abs(Start.Line - End.Line) > 1000) {
            options |= SyntaxHighlighter.RegexCompiledOption;
        }

        //
        foreach (var range in GetRanges(regexPattern, options)) {
            range.SetStyle(styleLayer);
        }

        //
        Tb.Invalidate();
    }

    /// <summary>
    /// Set style for given regex pattern
    /// </summary>
    public void SetStyle(StyleIndex styleLayer, Regex regex) {
        foreach (var range in GetRanges(regex)) {
            range.SetStyle(styleLayer);
        }

        //
        Tb.Invalidate();
    }

    /// <summary>
    /// Appends style to chars of range
    /// </summary>
    public void SetStyle(StyleIndex styleIndex) {
        //set code to chars
        int fromLine = Math.Min(End.Line, Start.Line);
        int toLine = Math.Max(End.Line, Start.Line);
        int fromChar = FromX;
        int toChar = ToX;
        if (fromLine < 0) {
            return;
        }

        //
        for (int y = fromLine; y <= toLine; y++) {
            int fromX = y == fromLine ? fromChar : 0;
            int toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
            for (int x = fromX; x <= toX; x++) {
                StudioChar c = Tb[y][x];
                c.Style |= styleIndex;
                Tb[y][x] = c;
            }
        }
    }

    /// <summary>
    /// Sets folding markers
    /// </summary>
    /// <param name="startFoldingPattern">Pattern for start folding line</param>
    /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
    public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern) {
        SetFoldingMarkers(startFoldingPattern, finishFoldingPattern, SyntaxHighlighter.RegexCompiledOption);
    }

    /// <summary>
    /// Sets folding markers
    /// </summary>
    /// <param name="startFoldingPattern">Pattern for start folding line</param>
    /// <param name="finishFoldingPattern">Pattern for finish folding line</param>
    public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern, RegexOptions options) {
        if (startFoldingPattern == finishFoldingPattern) {
            SetFoldingMarkers(startFoldingPattern, options);
            return;
        }

        foreach (var range in GetRanges(startFoldingPattern, options)) {
            Tb[range.Start.Line].FoldingStartMarker = startFoldingPattern;
        }

        foreach (var range in GetRanges(finishFoldingPattern, options)) {
            Tb[range.Start.Line].FoldingEndMarker = startFoldingPattern;
        }

        //
        Tb.Invalidate();
    }

    /// <summary>
    /// Sets folding markers
    /// </summary>
    /// <param name="startEndFoldingPattern">Pattern for start and end folding line</param>
    public void SetFoldingMarkers(string foldingPattern, RegexOptions options) {
        foreach (var range in GetRanges(foldingPattern, options)) {
            if (range.Start.Line > 0) {
                Tb[range.Start.Line - 1].FoldingEndMarker = foldingPattern;
            }

            Tb[range.Start.Line].FoldingStartMarker = foldingPattern;
        }

        Tb.Invalidate();
    }

    /// <summary>
    /// Finds ranges for given regex pattern
    /// </summary>
    /// <param name="regexPattern">Regex pattern</param>
    /// <returns>Enumeration of ranges</returns>
    public IEnumerable<Range> GetRanges(string regexPattern) {
        return GetRanges(regexPattern, RegexOptions.None);
    }

    /// <summary>
    /// Finds ranges for given regex pattern
    /// </summary>
    /// <param name="regexPattern">Regex pattern</param>
    /// <returns>Enumeration of ranges</returns>
    public IEnumerable<Range> GetRanges(string regexPattern, RegexOptions options) {
        //get text
        GetText(out string text, out List<Place> charIndexToPlace);
        //create regex
        Regex regex = new(regexPattern, options);
        //
        foreach (Match m in regex.Matches(text)) {
            Range r = new(Tb);
            //try get 'range' group, otherwise use group 0
            Group group = m.Groups["range"];
            if (!group.Success) {
                @group = m.Groups[0];
            }

            //
            r.Start = charIndexToPlace[group.Index];
            r.End = charIndexToPlace[group.Index + group.Length];
            yield return r;
        }
    }

    /// <summary>
    /// Finds ranges for given regex pattern.
    /// Search is separately in each line.
    /// This method requires less memory than GetRanges().
    /// </summary>
    /// <param name="regexPattern">Regex pattern</param>
    /// <returns>Enumeration of ranges</returns>
    public IEnumerable<Range> GetRangesByLines(string regexPattern, RegexOptions options) {
        Normalize();
        //create regex
        Regex regex = new(regexPattern, options);
        //
        var fts = Tb.TextSource as FileTextSource; //<----!!!! ugly
        //enumaerate lines
        for (int iLine = Start.Line; iLine <= End.Line; iLine++) {
            //
            bool isLineLoaded = fts != null ? fts.IsLineLoaded(iLine) : true;
            //
            var r = new Range(Tb, new Place(0, iLine), new Place(Tb[iLine].Count, iLine));
            if (iLine == Start.Line || iLine == End.Line) {
                r = r.GetIntersectionWith(this);
            }

            foreach (var foundRange in r.GetRanges(regex)) {
                yield return foundRange;
            }

            if (!isLineLoaded) {
                fts.UnloadLine(iLine);
            }
        }
    }

    /// <summary>
    /// Finds ranges for given regex
    /// </summary>
    /// <returns>Enumeration of ranges</returns>
    public IEnumerable<Range> GetRanges(Regex regex) {
        //get text
        GetText(out string text, out List<Place> charIndexToPlace);
        //
        foreach (Match m in regex.Matches(text)) {
            Range r = new(Tb);
            //try get 'range' group, otherwise use group 0
            Group group = m.Groups["range"];
            if (!group.Success) {
                @group = m.Groups[0];
            }

            //
            r.Start = charIndexToPlace[group.Index];
            r.End = charIndexToPlace[group.Index + group.Length];
            yield return r;
        }
    }

    /// <summary>
    /// Clear styles of range
    /// </summary>
    public void ClearStyle(params Style[] styles) {
        try {
            ClearStyle(Tb.GetStyleIndexMask(styles));
        } catch {
            // ignore
        }
    }

    /// <summary>
    /// Clear styles of range
    /// </summary>
    public void ClearStyle(StyleIndex styleIndex) {
        //set code to chars
        int fromLine = Math.Min(End.Line, Start.Line);
        int toLine = Math.Max(End.Line, Start.Line);
        int fromChar = FromX;
        int toChar = ToX;
        if (fromLine < 0) {
            return;
        }

        //
        for (int y = fromLine; y <= toLine; y++) {
            int fromX = y == fromLine ? fromChar : 0;
            int toX = y == toLine ? Math.Min(toChar - 1, Tb[y].Count - 1) : Tb[y].Count - 1;
            for (int x = fromX; x <= toX; x++) {
                StudioChar c = Tb[y][x];
                c.Style &= ~styleIndex;
                Tb[y][x] = c;
            }
        }

        //
        Tb.Invalidate();
    }

    /// <summary>
    /// Clear folding markers of all lines of range
    /// </summary>
    public void ClearFoldingMarkers() {
        //set code to chars
        int fromLine = Math.Min(End.Line, Start.Line);
        int toLine = Math.Max(End.Line, Start.Line);
        if (fromLine < 0) {
            return;
        }

        //
        for (int y = fromLine; y <= toLine; y++) {
            Tb[y].ClearFoldingMarkers();
        }

        //
        Tb.Invalidate();
    }

    void OnSelectionChanged() {
        //clear cache
        cachedTextVersion = -1;
        cachedText = null;
        cachedCharIndexToPlace = null;
        //
        if (Tb.Selection == this) {
            if (updating == 0) {
                Tb.OnSelectionChanged();
            }
        }
    }

    /// <summary>
    /// Starts selection position updating
    /// </summary>
    public void BeginUpdate() {
        updating++;
    }

    /// <summary>
    /// Ends selection position updating
    /// </summary>
    public void EndUpdate() {
        updating--;
        if (updating == 0) {
            OnSelectionChanged();
        }
    }

    public override string ToString() {
        return "Start: " + Start + " End: " + End;
    }

    /// <summary>
    /// Exchanges Start and End if End appears before Start
    /// </summary>
    public void Normalize() {
        if (Start > End) {
            Inverse();
        }
    }

    /// <summary>
    /// Exchanges Start and End
    /// </summary>
    public void Inverse() {
        var temp = start;
        start = end;
        end = temp;
    }

    /// <summary>
    /// Expands range from first char of Start line to last char of End line
    /// </summary>
    public void Expand() {
        Normalize();
        start = new Place(0, start.Line);
        end = new Place(Tb.GetLineLength(end.Line), end.Line);
    }

    /// <summary>
    /// Get fragment of text around Start place. Returns maximal mathed to pattern fragment.
    /// </summary>
    /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
    /// <returns>Range of found fragment</returns>
    public Range GetFragment(string allowedSymbolsPattern) {
        return GetFragment(allowedSymbolsPattern, RegexOptions.None);
    }

    /// <summary>
    /// Get fragment of text around Start place. Returns maximal mathed to pattern fragment.
    /// </summary>
    /// <param name="allowedSymbolsPattern">Allowed chars pattern for fragment</param>
    /// <returns>Range of found fragment</returns>
    public Range GetFragment(string allowedSymbolsPattern, RegexOptions options) {
        Range r = new(Tb);
        r.Start = Start;
        Regex regex = new(allowedSymbolsPattern, options);
        //go left, check symbols
        while (r.GoLeftThroughFolded()) {
            if (!regex.IsMatch(r.CharAfterStart.ToString())) {
                r.GoRightThroughFolded();
                break;
            }
        }

        Place startFragment = r.Start;

        r.Start = Start;
        //go right, check symbols
        do {
            if (!regex.IsMatch(r.CharAfterStart.ToString())) {
                break;
            }
        } while (r.GoRightThroughFolded());

        Place endFragment = r.Start;

        return new Range(Tb, startFragment, endFragment);
    }

    bool IsIdentifierChar(char c) {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    public void GoWordLeft(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start > end) {
                Start = End;
                return;
            }
        }

        Range range = Clone(); //for OnSelectionChanged disable

        Place prev;
        bool findIdentifier = IsIdentifierChar(range.CharBeforeStart);

        do {
            prev = range.Start;
            if (IsIdentifierChar(range.CharBeforeStart) ^ findIdentifier) {
                break;
            }

            //move left
            range.GoLeft(shift);
        } while (prev != range.Start);

        Start = range.Start;
        End = range.End;

        if (Tb.LineInfos[Start.Line].VisibleState != VisibleState.Visible) {
            GoRight(shift);
        }
    }

    public void GoWordRight(bool shift) {
        ColumnSelectionMode = false;

        if (!shift) {
            if (start < end) {
                Start = End;
                return;
            }
        }

        Range range = Clone(); //for OnSelectionChanged disable

        Place prev;
        bool findIdentifier = IsIdentifierChar(range.CharAfterStart);

        do {
            prev = range.Start;
            if (IsIdentifierChar(range.CharAfterStart) ^ findIdentifier) {
                break;
            }

            //move right
            range.GoRight(shift);
        } while (prev != range.Start);

        Start = range.Start;
        End = range.End;

        if (Tb.LineInfos[Start.Line].VisibleState != VisibleState.Visible) {
            GoLeft(shift);
        }
    }

    internal void GoFirst(bool shift) {
        ColumnSelectionMode = false;

        start = new Place(0, 0);
        if (Tb.LineInfos[Start.Line].VisibleState != VisibleState.Visible) {
            GoRight(shift);
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    internal void GoLast(bool shift) {
        ColumnSelectionMode = false;

        start = new Place(Tb[Tb.LinesCount - 1].Count, Tb.LinesCount - 1);
        if (Tb.LineInfos[Start.Line].VisibleState != VisibleState.Visible) {
            GoLeft(shift);
        }

        if (!shift) {
            end = start;
        }

        OnSelectionChanged();
    }

    public static StyleIndex ToStyleIndex(int i) {
        return (StyleIndex) (1 << i);
    }

    public IEnumerable<Range> GetSubRanges(bool includeEmpty) {
        if (!ColumnSelectionMode) {
            yield return this;
            yield break;
        }

        var rect = Bounds;
        for (int y = rect.StartLine; y <= rect.EndLine; y++) {
            if (rect.StartChar > Tb[y].Count && !includeEmpty) {
                continue;
            }

            var r = new Range(Tb, rect.StartChar, y, Math.Min(rect.EndChar, Tb[y].Count), y);
            yield return r;
        }
    }

    #region ColumnSelectionMode

    private Range GetIntersectionWith_ColumnSelectionMode(Range range) {
        if (range.Start.Line != range.End.Line) {
            return new Range(Tb, Start, Start);
        }

        var rect = Bounds;
        if (range.Start.Line < rect.StartLine || range.Start.Line > rect.EndLine) {
            return new Range(Tb, Start, Start);
        }

        return new Range(Tb, rect.StartChar, range.Start.Line, rect.EndChar, range.Start.Line).GetIntersectionWith(range);
    }

    private bool GoRightThroughFolded_ColumnSelectionMode() {
        var boundes = Bounds;
        bool endOfLines = true;
        for (int iLine = boundes.StartLine; iLine <= boundes.EndLine; iLine++) {
            if (boundes.EndChar < Tb[iLine].Count) {
                endOfLines = false;
                break;
            }
        }

        if (endOfLines) {
            return false;
        }

        var start = Start;
        var end = End;
        start.Offset(1, 0);
        end.Offset(1, 0);
        BeginUpdate();
        Start = start;
        End = end;
        EndUpdate();

        return true;
    }

    private IEnumerable<Place> GetEnumerator_ColumnSelectionMode() {
        var bounds = Bounds;
        if (bounds.StartLine < 0) {
            yield break;
        }

        //
        for (int y = bounds.StartLine; y <= bounds.EndLine; y++) {
            for (int x = bounds.StartChar; x < bounds.EndChar; x++) {
                if (x < Tb[y].Count) {
                    yield return new Place(x, y);
                }
            }
        }
    }

    private string Text_ColumnSelectionMode {
        get {
            StringBuilder sb = new();
            var bounds = Bounds;
            if (bounds.StartLine < 0) {
                return "";
            }

            //
            for (int y = bounds.StartLine; y <= bounds.EndLine; y++) {
                for (int x = bounds.StartChar; x < bounds.EndChar; x++) {
                    if (x < Tb[y].Count) {
                        sb.Append(Tb[y][x].Char_);
                    }
                }

                if (bounds.EndLine != bounds.StartLine && y != bounds.EndLine) {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }

    internal void GoDown_ColumnSelectionMode() {
        int iLine = Tb.FindNextVisibleLine(End.Line);
        End = new Place(End.Char, iLine);
    }

    internal void GoUp_ColumnSelectionMode() {
        int iLine = Tb.FindPrevVisibleLine(End.Line);
        End = new Place(End.Char, iLine);
    }

    internal void GoRight_ColumnSelectionMode() {
        End = new Place(End.Char + 1, End.Line);
    }

    internal void GoLeft_ColumnSelectionMode() {
        if (End.Char > 0) {
            End = new Place(End.Char - 1, End.Line);
        }
    }

    #endregion
}

public struct RangeRect {
    public RangeRect(int startLine, int startChar, int endLine, int endChar) {
        StartLine = startLine;
        StartChar = startChar;
        EndLine = endLine;
        EndChar = endChar;
    }

    public int StartLine;
    public int StartChar;
    public int EndLine;
    public int EndChar;
}