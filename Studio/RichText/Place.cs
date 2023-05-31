namespace CelesteStudio.RichText;

public struct Place {
    public int Char;
    public int Line;

    public Place(int char_, int line) {
        Char = char_;
        Line = line;
    }

    public void Offset(int dx, int dy) {
        Char += dx;
        Line += dy;
    }

    public override int GetHashCode() {
        return Char ^ Line;
    }

    public override bool Equals(object obj) {
        return obj is Place place && place.Line == Line && place.Char == Char;
    }

    public static bool operator !=(Place p1, Place p2) => !p1.Equals(p2);

    public static bool operator ==(Place p1, Place p2) => p1.Equals(p2);

    public static bool operator <(Place p1, Place p2) {
        if (p1.Line < p2.Line) {
            return true;
        }

        if (p1.Line > p2.Line) {
            return false;
        }

        if (p1.Char < p2.Char) {
            return true;
        }

        return false;
    }

    public static bool operator <=(Place p1, Place p2) {
        if (p1.Equals(p2)) {
            return true;
        }

        if (p1.Line < p2.Line) {
            return true;
        }

        if (p1.Line > p2.Line) {
            return false;
        }

        if (p1.Char < p2.Char) {
            return true;
        }

        return false;
    }

    public static bool operator >(Place p1, Place p2) {
        if (p1.Line > p2.Line) {
            return true;
        }

        if (p1.Line < p2.Line) {
            return false;
        }

        if (p1.Char > p2.Char) {
            return true;
        }

        return false;
    }

    public static bool operator >=(Place p1, Place p2) {
        if (p1.Equals(p2)) {
            return true;
        }

        if (p1.Line > p2.Line) {
            return true;
        }

        if (p1.Line < p2.Line) {
            return false;
        }

        if (p1.Char > p2.Char) {
            return true;
        }

        return false;
    }

    public static Place Empty => new();

    public override string ToString() {
        return "(" + Char + "," + Line + ")";
    }
}