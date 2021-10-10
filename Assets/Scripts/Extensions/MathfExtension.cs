using UnityEngine;

public static class MathfExtension
{
    public static int RoundUpToPowerOfTwo(int value)
    {
        return 1 << Mathf.CeilToInt(Mathf.Log(value,2));
    }
}