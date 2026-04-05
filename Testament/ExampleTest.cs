namespace IdyllicMultiplayerProject.Testament;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite][RequireGodotRuntime]
public class GdUnitExampleTest
{ 
    [TestCase]
    public void StringToLower() {
        AssertString("AbcD".ToLower()).IsEqual("abcd");
    }
   
    [TestCase]
    public void StringToUpper() {
        AssertString("AbcD".ToUpper()).IsEqual("ABCD");
    }
}