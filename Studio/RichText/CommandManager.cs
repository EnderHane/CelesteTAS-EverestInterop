using System;
using System.Collections.Generic;

namespace CelesteStudio.RichText;

internal class CommandManager {
    readonly LimitedStack<UndoableCommand> history;
    readonly int maxHistoryLength = 1000;
    readonly Stack<UndoableCommand> redoStack = new();

    int autoUndoCommands = 0;

    int disabledCommands = 0;

    public CommandManager(TextSource ts) {
        history = new LimitedStack<UndoableCommand>(maxHistoryLength);
        TextSource = ts;
    }

    public TextSource TextSource { get; private set; }

    public bool UndoEnabled => history.Count > 0;

    public bool RedoEnabled => redoStack.Count > 0;

    public void ExecuteCommand(Command cmd) {
        if (disabledCommands > 0) {
            return;
        }

        //multirange ?
        if (cmd.Ts.CurrentTB.Selection.ColumnSelectionMode) {
            if (cmd is UndoableCommand command) {
                //make wrapper
                cmd = new MultiRangeCommand(command);
            }
        }


        if (cmd is UndoableCommand) {
            //if range is ColumnRange, then create wrapper
            (cmd as UndoableCommand).AutoUndo = autoUndoCommands > 0;
            history.Push(cmd as UndoableCommand);
        }

        try {
            cmd.Execute();
        } catch (ArgumentOutOfRangeException) {
            //OnTextChanging cancels enter of the text
            if (cmd is UndoableCommand) {
                history.Pop();
            }
        }

        //
        redoStack.Clear();
        //
        TextSource.CurrentTB.OnUndoRedoStateChanged();
    }

    public void Undo() {
        if (history.Count > 0) {
            var cmd = history.Pop();

            BeginDisableCommands(); //prevent text changing into handlers
            try {
                cmd.Undo();
            } finally {
                EndDisableCommands();
            }

            redoStack.Push(cmd);
        }

        //undo next autoUndo command
        if (history.Count > 0) {
            UndoableCommand cmd = history.Peek();
            if (cmd.AutoUndo || cmd is InsertCharCommand) {
                Undo();
            }
        }

        TextSource.CurrentTB.OnUndoRedoStateChanged();
    }

    private void EndDisableCommands() {
        disabledCommands--;
    }

    private void BeginDisableCommands() {
        disabledCommands++;
    }

    public void EndAutoUndoCommands() {
        autoUndoCommands--;
        if (autoUndoCommands == 0) {
            if (history.Count > 0) {
                history.Peek().AutoUndo = false;
            }
        }
    }

    public void BeginAutoUndoCommands() {
        autoUndoCommands++;
    }

    internal void ClearHistory() {
        history.Clear();
        redoStack.Clear();
        TextSource.CurrentTB.OnUndoRedoStateChanged();
    }

    internal void Redo() {
        if (redoStack.Count == 0) {
            return;
        }

        UndoableCommand cmd;
        BeginDisableCommands(); //prevent text changing into handlers
        try {
            cmd = redoStack.Pop();
            if (TextSource.CurrentTB.Selection.ColumnSelectionMode) {
                TextSource.CurrentTB.Selection.ColumnSelectionMode = false;
            }

            TextSource.CurrentTB.Selection.Start = cmd.Sel.Start;
            TextSource.CurrentTB.Selection.End = cmd.Sel.End;
            cmd.Execute();
            history.Push(cmd);
        } finally {
            EndDisableCommands();
        }

        //redo command after autoUndoable command
        if (cmd.AutoUndo) {
            Redo();
        }

        TextSource.CurrentTB.OnUndoRedoStateChanged();
    }
}

internal abstract class Command {
    internal TextSource Ts;
    public abstract void Execute();
}

internal class RangeInfo {
    public RangeInfo(Range r) {
        Start = r.Start;
        End = r.End;
    }

    public Place Start { get; set; }
    public Place End { get; set; }

    internal int FromX {
        get {
            if (End.Line < Start.Line) {
                return End.Char;
            }

            if (End.Line > Start.Line) {
                return Start.Char;
            }

            return Math.Min(End.Char, Start.Char);
        }
    }
}

internal abstract class UndoableCommand : Command {
    internal bool AutoUndo;
    internal RangeInfo LastSel;
    internal RangeInfo Sel;

    public UndoableCommand(TextSource ts) {
        Ts = ts;
        Sel = new RangeInfo(ts.CurrentTB.Selection);
    }

    public virtual void Undo() {
        OnTextChanged(true);
    }

    public override void Execute() {
        LastSel = new RangeInfo(Ts.CurrentTB.Selection);
        OnTextChanged(false);
    }

    protected virtual void OnTextChanged(bool invert) {
        bool b = Sel.Start.Line < LastSel.Start.Line;
        if (invert) {
            if (b) {
                Ts.OnTextChanged(Sel.Start.Line, Sel.Start.Line);
            } else {
                Ts.OnTextChanged(Sel.Start.Line, LastSel.Start.Line);
            }
        } else {
            if (b) {
                Ts.OnTextChanged(Sel.Start.Line, LastSel.Start.Line);
            } else {
                Ts.OnTextChanged(LastSel.Start.Line, LastSel.Start.Line);
            }
        }
    }

    public abstract UndoableCommand Clone();
}