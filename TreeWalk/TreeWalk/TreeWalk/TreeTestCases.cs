namespace TreeWalk;

public static class TreeTestCases
{
    public static IEnumerable<object[]> GetCases()
    {
        // ---------- T1 ----------
        yield return new object[]
        {
            "A,B,C,,D,E,F,,,,,,,G",
            "B", "F", 3,
            new[] { "U", "DR", "DR" },
@"
       A
     /   \
    B     C
     \   / \
      D E   F
            /
           G
"
        };

        yield return new object[]
        {
            "A,B,C,,D,E,F,,,,,,,G",
            "E", "D", 4,
            new[] { "U", "U", "DL", "DR" },
@"
       A
     /   \
    B     C
     \   / \
      D E   F
            /
           G
"
        };

        // ---------- T2 ----------
        yield return new object[]
        {
            "A,B,C,D,E,F,G",
            "D", "G", 4,
            new[] { "U", "U", "DR", "DR" },
@"
       A
     /   \
    B     C
   / \   / \
  D   E F   G
"
        };

        // ---------- T3 ----------
        yield return new object[]
        {
            "A,B,,C,,,,D",
            "A", "D", 3,
            new[] { "DL", "DL", "DL" },
@"
    A
   /
  B
 /
C
/
D
"
        };

        // ---------- T4 ----------
        yield return new object[]
        {
            "A,,B,,,,C,,,,,,D",
            "A", "D", 3,
            new[] { "DR", "DR", "DR" },
@"
A
 \
  B
   \
    C
     \
      D
"
        };

        // ---------- T5 ----------
        yield return new object[]
        {
            "A",
            "A", "A", 0,
            new string[] { },
@"
A
"
        };

        // ---------- T8 (深度 10 左偏) ----------
        yield return new object[]
        {
            "A,B,,,,,,,,,C,,,,,,,,,D,,,,,,,,,E,,,,,,,,,F,,,,,,,,,G,,,,,,,,,H,,,,,,,,,I,,,,,,,,,J",
            "A", "J", 9,
            new[] { "DL","DL","DL","DL","DL","DL","DL","DL","DL" },
@"
A
/
B
/
C
/
D
/
E
/
F
/
G
/
H
/
I
/
J
"
        };

        // ---------- T9 (深度 10 右偏) ----------
        yield return new object[]
        {
            "A,,B,,,,C,,,,,,D,,,,,,,,E,,,,,,,,,,F,,,,,,,,,,,,G,,,,,,,,,,,,,,H,,,,,,,,,,,,,,,,I,,,,,,,,,,,,,,,,,,J",
            "A", "J", 9,
            new[] { "DR","DR","DR","DR","DR","DR","DR","DR","DR" },
@"
A
 \
  B
   \
    C
     \
      D
       \
        E
         \
          F
           \
            G
             \
              H
               \
                I
                 \
                  J
"
        };

        // ---------- T10 (深度 10 Zig-Zag) ----------
        yield return new object[]
        {
            "A,B,,,C,,,,,D,,,,,,,E,,,,,,,,,F,,,,,,,,,,,G,,,,,,,,,,,,,H,,,,,,,,,,,,,,,I,,,,,,,,,,,,,,,,,J",
            "A", "J", 9,
            new[] { "DL","DR","DL","DR","DL","DR","DL","DR","DL" },
@"
A
/
B
 \
  C
 /
D
 \
  E
 /
F
 \
  G
 /
H
 \
  I
 /
J
"
        };
    }
}
