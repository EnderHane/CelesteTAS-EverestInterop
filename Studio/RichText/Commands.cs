using System;
using System.Collections.Generic;

namespace CelesteStudio.RichText;

/// <summary>
/// Insert single char
/// </summary>
/// <remarks>This operation includes also insertion of new line and removing char by backspace</remarks>
internal class InsertCharCommand : UndoableCommand {
    internal char Char_;
    private char deletedChar = '\x0';

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tb">Underlaying textbox</param>
    /// <param name="c">Inserting char</param>
    public InsertCharCommand(TextSource ts, char c)
        : base(ts) {
        Char_ = c;
    }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        Ts.OnTextChanging();
        switch (Char_) {
            case '\n':
                MergeLines(Sel.Start.Line, Ts);
                break;
            case '\r':
                break;
            case (char) 1:
            case '\b':
                Ts.CurrentTB.Selection.Start = LastSel.Start;
                char cc = '\x0';
                if (deletedChar != '\x0') {
                    Ts.CurrentTB.ExpandBlock(Ts.CurrentTB.Selection.Start.Line);
                    InsertChar(deletedChar, ref cc, Ts);
                }

                break;
            default:
                Ts.CurrentTB.ExpandBlock(Sel.Start.Line);
                Ts[Sel.Start.Line].RemoveAt(Sel.Start.Char);
                Ts.CurrentTB.Selection.Start = Sel.Start;
                break;
        }

        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Sel.Start.Line, Sel.Start.Line));

        base.Undo();
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        Ts.CurrentTB.ExpandBlock(Ts.CurrentTB.Selection.Start.Line);
        string s = Char_.ToString();
        Ts.OnTextChanging(ref s);
        if (s.Length == 1) {
            Char_ = s[0];
        }

        if (String.IsNullOrEmpty(s)) {
            throw new ArgumentOutOfRangeException();
        }


        if (Ts.Count == 0) {
            InsertLine(Ts);
        }

        InsertChar(Char_, ref deletedChar, Ts);

        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(Ts.CurrentTB.Selection.Start.Line, Ts.CurrentTB.Selection.Start.Line));
        base.Execute();
    }

    internal static void InsertChar(char c, ref char deletedChar, TextSource ts) {
        var tb = ts.CurrentTB;

        switch (c) {
            case '\n':
                if (!ts.CurrentTB.AllowInsertRemoveLines) {
                    throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                }

                if (ts.Count == 0) {
                    InsertLine(ts);
                }

                InsertLine(ts);
                break;
            case '\r':
                break;
            case (char) 1:
            case '\b': //backspace
                if (tb.Selection.Start.Char == 0 && tb.Selection.Start.Line == 0) {
                    return;
                }

                if (tb.Selection.Start.Char == 0) {
                    if (!ts.CurrentTB.AllowInsertRemoveLines) {
                        throw new ArgumentOutOfRangeException("Cant insert this char in ColumnRange mode");
                    }

                    if (tb.LineInfos[tb.Selection.Start.Line - 1].VisibleState != VisibleState.Visible) {
                        tb.ExpandBlock(tb.Selection.Start.Line - 1);
                    }

                    deletedChar = '\n';
                    MergeLines(tb.Selection.Start.Line - 1, ts);
                } else {
                    int tl = tb.TabLength;
                    deletedChar = ts[tb.Selection.Start.Line][tb.Selection.Start.Char - 1].Char_;
                    do {
                        ts[tb.Selection.Start.Line].RemoveAt(tb.Selection.Start.Char - 1);
                        tb.Selection.Start = new Place(tb.Selection.Start.Char + (c == (char) 1 ? 0 : -1), tb.Selection.Start.Line);
                        tl--;
                        if (c == (char) 1 && tb.Selection.Start.Char > 0 && tb.Selection.Start.Char >= ts[tb.Selection.Start.Line].Count) {
                            tb.Selection.Start = new Place(tb.Selection.Start.Char - 1, tb.Selection.Start.Line);
                            break;
                        }

                        if (deletedChar != ' ' || tb.Selection.Start.Char == 0) {
                            if (c == (char) 1 && tb.Selection.Start.Char > 0) {
                                tb.Selection.Start = new Place(tb.Selection.Start.Char - 1, tb.Selection.Start.Line);
                            }

                            break;
                        }

                        deletedChar = ts[tb.Selection.Start.Line][tb.Selection.Start.Char - 1].Char_;
                        if ((deletedChar != ' ' || tl == 0) && c == (char) 1 && tb.Selection.Start.Char > 0) {
                            tb.Selection.Start = new Place(tb.Selection.Start.Char - 1, tb.Selection.Start.Line);
                        }
                    } while (deletedChar == ' ' && tl > 0);
                }

                break;
            case '\t':
                for (int i = 0; i < tb.TabLength; i++) {
                    ts[tb.Selection.Start.Line].Insert(tb.Selection.Start.Char, new StudioChar(' '));
                }

                tb.Selection.Start = new Place(tb.Selection.Start.Char + tb.TabLength, tb.Selection.Start.Line);
                break;
            default:
                ts[tb.Selection.Start.Line].Insert(tb.Selection.Start.Char, new StudioChar(c));
                tb.Selection.Start = new Place(tb.Selection.Start.Char + 1, tb.Selection.Start.Line);
                break;
        }
    }

    internal static void InsertLine(TextSource ts) {
        var tb = ts.CurrentTB;

        if (!tb.Multiline && tb.LinesCount > 0) {
            return;
        }

        if (ts.Count == 0) {
            ts.InsertLine(0, ts.CreateLine());
        } else {
            BreakLines(tb.Selection.Start.Line, tb.Selection.Start.Char, ts);
        }

        tb.Selection.Start = new Place(0, tb.Selection.Start.Line + 1);
        ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
    }

    /// <summary>
    /// Merge lines i and i+1
    /// </summary>
    internal static void MergeLines(int i, TextSource ts) {
        var tb = ts.CurrentTB;

        if (i + 1 >= ts.Count) {
            return;
        }

        tb.ExpandBlock(i);
        tb.ExpandBlock(i + 1);
        int pos = ts[i].Count;
        //
        if (ts[i].Count == 0) {
            ts.RemoveLine(i);
        } else if (ts[i + 1].Count == 0) {
            ts.RemoveLine(i + 1);
        } else {
            ts[i].AddRange(ts[i + 1]);
            ts.RemoveLine(i + 1);
        }

        tb.Selection.Start = new Place(pos, i);
        ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
    }

    internal static void BreakLines(int iLine, int pos, TextSource ts) {
        Line newLine = ts.CreateLine();
        for (int i = pos; i < ts[iLine].Count; i++) {
            newLine.Add(ts[iLine][i]);
        }

        ts[iLine].RemoveRange(pos, ts[iLine].Count - pos);
        //
        ts.InsertLine(iLine + 1, newLine);
    }

    public override UndoableCommand Clone() {
        return new InsertCharCommand(Ts, Char_);
    }
}

/// <summary>
/// Insert text
/// </summary>
internal class InsertTextCommand : UndoableCommand {
    internal string InsertedText;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tb">Underlaying textbox</param>
    /// <param name="insertedText">Text for inserting</param>
    public InsertTextCommand(TextSource ts, string insertedText)
        : base(ts) {
        this.InsertedText = insertedText;
    }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        Ts.CurrentTB.Selection.Start = Sel.Start;
        Ts.CurrentTB.Selection.End = LastSel.Start;
        Ts.OnTextChanging();
        ClearSelectedCommand.ClearSelected(Ts);
        base.Undo();
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        Ts.OnTextChanging(ref InsertedText);
        InsertText(InsertedText, Ts);
        base.Execute();
    }

    internal static void InsertText(string insertedText, TextSource ts) {
        var tb = ts.CurrentTB;
        try {
            tb.Selection.BeginUpdate();
            char cc = '\x0';
            if (ts.Count == 0) {
                InsertCharCommand.InsertLine(ts);
            }

            tb.ExpandBlock(tb.Selection.Start.Line);
            foreach (char c in insertedText) {
                InsertCharCommand.InsertChar(c, ref cc, ts);
            }

            ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
        } finally {
            tb.Selection.EndUpdate();
        }
    }

    public override UndoableCommand Clone() {
        return new InsertTextCommand(Ts, InsertedText);
    }
}

/// <summary>
/// Insert text into given ranges
/// </summary>
internal class ReplaceTextCommand : UndoableCommand {
    readonly List<string> prevText = new();
    readonly List<Range> ranges;
    string insertedText;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tb">Underlaying textbox</param>
    /// <param name="ranges">List of ranges for replace</param>
    /// <param name="insertedText">Text for inserting</param>
    public ReplaceTextCommand(TextSource ts, List<Range> ranges, string insertedText)
        : base(ts) {
        //sort ranges by place
        ranges.Sort((r1, r2) => {
            if (r1.Start.Line == r2.Start.Line) {
                return r1.Start.Char.CompareTo(r2.Start.Char);
            }

            return r1.Start.Line.CompareTo(r2.Start.Line);
        });
        //
        this.ranges = ranges;
        this.insertedText = insertedText;
        LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
    }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        var tb = Ts.CurrentTB;

        Ts.OnTextChanging();

        tb.Selection.BeginUpdate();
        for (int i = 0; i < ranges.Count; i++) {
            tb.Selection.Start = ranges[i].Start;
            for (int j = 0; j < insertedText.Length; j++) {
                tb.Selection.GoRight(true);
            }

            ClearSelectedCommand.ClearSelected(Ts);
            InsertTextCommand.InsertText(prevText[prevText.Count - i - 1], Ts);
            Ts.OnTextChanged(ranges[i].Start.Line, ranges[i].Start.Line);
        }

        tb.Selection.EndUpdate();

        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        var tb = Ts.CurrentTB;
        prevText.Clear();

        Ts.OnTextChanging(ref insertedText);

        tb.Selection.BeginUpdate();
        for (int i = ranges.Count - 1; i >= 0; i--) {
            tb.Selection.Start = ranges[i].Start;
            tb.Selection.End = ranges[i].End;
            prevText.Add(tb.Selection.Text);
            ClearSelectedCommand.ClearSelected(Ts);
            InsertTextCommand.InsertText(insertedText, Ts);
            Ts.OnTextChanged(ranges[i].Start.Line, ranges[i].End.Line);
        }

        tb.Selection.EndUpdate();
        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

        LastSel = new RangeInfo(tb.Selection);
    }

    public override UndoableCommand Clone() {
        return new ReplaceTextCommand(Ts, new List<Range>(ranges), insertedText);
    }
}

/// <summary>
/// Clear selected text
/// </summary>
internal class ClearSelectedCommand : UndoableCommand {
    string deletedText;

    /// <summary>
    /// Construstor
    /// </summary>
    /// <param name="tb">Underlaying textbox</param>
    public ClearSelectedCommand(TextSource ts)
        : base(ts) { }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        Ts.CurrentTB.Selection.Start = new Place(Sel.FromX, Math.Min(Sel.Start.Line, Sel.End.Line));
        Ts.OnTextChanging();
        InsertTextCommand.InsertText(deletedText, Ts);
        Ts.OnTextChanged(Sel.Start.Line, Sel.End.Line);
        Ts.CurrentTB.Selection.Start = Sel.Start;
        Ts.CurrentTB.Selection.End = Sel.End;
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        var tb = Ts.CurrentTB;

        string temp = null;
        Ts.OnTextChanging(ref temp);
        if (temp == "") {
            throw new ArgumentOutOfRangeException();
        }

        deletedText = tb.Selection.Text;
        ClearSelected(Ts);
        LastSel = new RangeInfo(tb.Selection);
        Ts.OnTextChanged(LastSel.Start.Line, LastSel.Start.Line);
    }

    internal static void ClearSelected(TextSource ts) {
        var tb = ts.CurrentTB;

        Place start = tb.Selection.Start;
        Place end = tb.Selection.End;
        int fromLine = Math.Min(end.Line, start.Line);
        int toLine = Math.Max(end.Line, start.Line);
        int fromChar = tb.Selection.FromX;
        int toChar = tb.Selection.ToX;
        if (fromLine < 0) {
            return;
        }

        //
        if (fromLine == toLine) {
            ts[fromLine].RemoveRange(fromChar, toChar - fromChar);
        } else {
            ts[fromLine].RemoveRange(fromChar, ts[fromLine].Count - fromChar);
            ts[toLine].RemoveRange(0, toChar);
            ts.RemoveLine(fromLine + 1, toLine - fromLine - 1);
            InsertCharCommand.MergeLines(fromLine, ts);
        }

        //
        tb.Selection.Start = new Place(fromChar, fromLine);
        //
        ts.NeedRecalc(new TextSource.TextChangedEventArgs(fromLine, toLine));
    }

    public override UndoableCommand Clone() {
        return new ClearSelectedCommand(Ts);
    }
}

/// <summary>
/// Replaces text
/// </summary>
internal class ReplaceMultipleTextCommand : UndoableCommand {
    readonly List<string> prevText = new();
    readonly List<ReplaceRange> ranges;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ts">Underlaying textsource</param>
    /// <param name="ranges">List of ranges for replace</param>
    public ReplaceMultipleTextCommand(TextSource ts, List<ReplaceRange> ranges)
        : base(ts) {
        //sort ranges by place
        ranges.Sort((r1, r2) => {
            if (r1.ReplacedRange.Start.Line == r2.ReplacedRange.Start.Line) {
                return r1.ReplacedRange.Start.Char.CompareTo(r2.ReplacedRange.Start.Char);
            }

            return r1.ReplacedRange.Start.Line.CompareTo(r2.ReplacedRange.Start.Line);
        });
        //
        this.ranges = ranges;
        LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
    }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        var tb = Ts.CurrentTB;

        Ts.OnTextChanging();

        tb.Selection.BeginUpdate();
        for (int i = 0; i < ranges.Count; i++) {
            tb.Selection.Start = ranges[i].ReplacedRange.Start;
            for (int j = 0; j < ranges[i].ReplaceText.Length; j++) {
                tb.Selection.GoRight(true);
            }

            ClearSelectedCommand.ClearSelected(Ts);
            int prevTextIndex = ranges.Count - 1 - i;
            InsertTextCommand.InsertText(prevText[prevTextIndex], Ts);
            Ts.OnTextChanged(ranges[i].ReplacedRange.Start.Line, ranges[i].ReplacedRange.Start.Line);
        }

        tb.Selection.EndUpdate();

        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        var tb = Ts.CurrentTB;
        prevText.Clear();

        Ts.OnTextChanging();

        tb.Selection.BeginUpdate();
        for (int i = ranges.Count - 1; i >= 0; i--) {
            tb.Selection.Start = ranges[i].ReplacedRange.Start;
            tb.Selection.End = ranges[i].ReplacedRange.End;
            prevText.Add(tb.Selection.Text);
            ClearSelectedCommand.ClearSelected(Ts);
            InsertTextCommand.InsertText(ranges[i].ReplaceText, Ts);
            Ts.OnTextChanged(ranges[i].ReplacedRange.Start.Line, ranges[i].ReplacedRange.End.Line);
        }

        tb.Selection.EndUpdate();
        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

        LastSel = new RangeInfo(tb.Selection);
    }

    public override UndoableCommand Clone() {
        return new ReplaceMultipleTextCommand(Ts, new List<ReplaceRange>(ranges));
    }

    public class ReplaceRange {
        public Range ReplacedRange { get; set; }
        public String ReplaceText { get; set; }
    }
}

/// <summary>
/// Removes lines
/// </summary>
internal class RemoveLinesCommand : UndoableCommand {
    readonly List<int> iLines;
    readonly List<string> prevText = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tb">Underlaying textbox</param>
    /// <param name="ranges">List of ranges for replace</param>
    /// <param name="insertedText">Text for inserting</param>
    public RemoveLinesCommand(TextSource ts, List<int> iLines)
        : base(ts) {
        //sort iLines
        iLines.Sort();
        //
        this.iLines = iLines;
        LastSel = Sel = new RangeInfo(ts.CurrentTB.Selection);
    }

    /// <summary>
    /// Undo operation
    /// </summary>
    public override void Undo() {
        var tb = Ts.CurrentTB;

        Ts.OnTextChanging();

        tb.Selection.BeginUpdate();
        //tb.BeginUpdate();
        for (int i = 0; i < iLines.Count; i++) {
            int iLine = iLines[i];

            if (iLine < Ts.Count) {
                tb.Selection.Start = new Place(0, iLine);
            } else {
                tb.Selection.Start = new Place(Ts[Ts.Count - 1].Count, Ts.Count - 1);
            }

            InsertCharCommand.InsertLine(Ts);
            tb.Selection.Start = new Place(0, iLine);
            string text = prevText[prevText.Count - i - 1];
            InsertTextCommand.InsertText(text, Ts);
            Ts[iLine].IsChanged = true;
            if (iLine < Ts.Count - 1) {
                Ts[iLine + 1].IsChanged = true;
            } else {
                Ts[iLine - 1].IsChanged = true;
            }

            if (text.Trim() != string.Empty) {
                Ts.OnTextChanged(iLine, iLine);
            }
        }

        //tb.EndUpdate();
        tb.Selection.EndUpdate();

        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));
    }

    /// <summary>
    /// Execute operation
    /// </summary>
    public override void Execute() {
        var tb = Ts.CurrentTB;
        prevText.Clear();

        Ts.OnTextChanging();

        tb.Selection.BeginUpdate();
        for (int i = iLines.Count - 1; i >= 0; i--) {
            int iLine = iLines[i];

            prevText.Add(Ts[iLine].Text); //backward
            Ts.RemoveLine(iLine);
            //ts.OnTextChanged(ranges[i].Start.iLine, ranges[i].End.iLine);
        }

        tb.Selection.Start = new Place(0, 0);
        tb.Selection.EndUpdate();
        Ts.NeedRecalc(new TextSource.TextChangedEventArgs(0, 1));

        LastSel = new RangeInfo(tb.Selection);
    }

    public override UndoableCommand Clone() {
        return new RemoveLinesCommand(Ts, new List<int>(iLines));
    }
}

/// <summary>
/// Wrapper for multirange commands
/// </summary>
internal class MultiRangeCommand : UndoableCommand {
    private readonly UndoableCommand cmd;
    private readonly List<UndoableCommand> commandsByRanges = new();
    private readonly Range range;

    public MultiRangeCommand(UndoableCommand command)
        : base(command.Ts) {
        cmd = command;
        range = Ts.CurrentTB.Selection.Clone();
    }

    public override void Execute() {
        commandsByRanges.Clear();
        var prevSelection = range.Clone();
        int iChar = -1;
        int iStartLine = prevSelection.Start.Line;
        int iEndLine = prevSelection.End.Line;
        Ts.CurrentTB.Selection.ColumnSelectionMode = false;
        Ts.CurrentTB.Selection.BeginUpdate();
        Ts.CurrentTB.BeginUpdate();
        Ts.CurrentTB.AllowInsertRemoveLines = false;
        try {
            if (cmd is InsertTextCommand) {
                ExecuteInsertTextCommand(ref iChar, (cmd as InsertTextCommand).InsertedText);
            } else if (cmd is InsertCharCommand && (cmd as InsertCharCommand).Char_ != '\x0' && (cmd as InsertCharCommand).Char_ != '\b'
                      ) //if not DEL or BACKSPACE
            {
                ExecuteInsertTextCommand(ref iChar, (cmd as InsertCharCommand).Char_.ToString());
            } else {
                ExecuteCommand(ref iChar);
            }
        } catch (ArgumentOutOfRangeException) { } finally {
            Ts.CurrentTB.AllowInsertRemoveLines = true;
            Ts.CurrentTB.EndUpdate();

            Ts.CurrentTB.Selection = range;
            if (iChar >= 0) {
                Ts.CurrentTB.Selection.Start = new Place(iChar, iStartLine);
                Ts.CurrentTB.Selection.End = new Place(iChar, iEndLine);
            }

            Ts.CurrentTB.Selection.ColumnSelectionMode = true;
            Ts.CurrentTB.Selection.EndUpdate();
        }
    }

    private void ExecuteInsertTextCommand(ref int iChar, string text) {
        string[] lines = text.Split('\n');
        int iLine = 0;
        foreach (var r in range.GetSubRanges(true)) {
            var line = Ts.CurrentTB[r.Start.Line];
            bool lineIsEmpty = r.End < r.Start && line.StartSpacesCount == line.Count;
            if (!lineIsEmpty) {
                string insertedText = lines[iLine % lines.Length];
                if (r.End < r.Start && insertedText != "") {
                    //add forwarding spaces
                    insertedText = new string(' ', r.Start.Char - r.End.Char) + insertedText;
                    r.Start = r.End;
                }

                Ts.CurrentTB.Selection = r;
                var c = new InsertTextCommand(Ts, insertedText);
                c.Execute();
                if (Ts.CurrentTB.Selection.End.Char > iChar) {
                    iChar = Ts.CurrentTB.Selection.End.Char;
                }

                commandsByRanges.Add(c);
            }

            iLine++;
        }
    }

    private void ExecuteCommand(ref int iChar) {
        foreach (var r in range.GetSubRanges(false)) {
            Ts.CurrentTB.Selection = r;
            var c = cmd.Clone();
            c.Execute();
            if (Ts.CurrentTB.Selection.End.Char > iChar) {
                iChar = Ts.CurrentTB.Selection.End.Char;
            }

            commandsByRanges.Add(c);
        }
    }

    public override void Undo() {
        Ts.CurrentTB.BeginUpdate();
        Ts.CurrentTB.Selection.BeginUpdate();
        try {
            for (int i = commandsByRanges.Count - 1; i >= 0; i--) {
                commandsByRanges[i].Undo();
            }
        } finally {
            Ts.CurrentTB.Selection.EndUpdate();
            Ts.CurrentTB.EndUpdate();
        }

        Ts.CurrentTB.Selection = range.Clone();
        Ts.CurrentTB.OnTextChanged(range);
        Ts.CurrentTB.OnSelectionChanged();
        Ts.CurrentTB.Selection.ColumnSelectionMode = true;
    }

    public override UndoableCommand Clone() {
        throw new NotImplementedException();
    }
}