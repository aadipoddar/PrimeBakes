namespace PrimeBakesLibrary.Helper;

public class BoolBit
{
	public static bool ConvertToBool(int value) => value == 1;
	public static int ConvertToInt(bool value) => value ? 1 : 0;
}