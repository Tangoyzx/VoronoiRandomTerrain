using UnityEngine;
public static class Vector2Checker {
    public static bool isNull(Vector2 v)
    {
        return (v.x < 0 || v.y < 0);
    }

    public static Vector2 getNull() {
        return new Vector2(-1, -1);
    }
}