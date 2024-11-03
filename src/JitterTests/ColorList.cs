using Jitter2.DataStructures;

namespace JitterTests;

public class ColorList
{
    [SetUp]
    public void Setup()
    {
    }

    public struct Entry
    {
        private int _internal;
        public int EntryColor;

        public Entry(int color)
        {
            EntryColor = color;
        }
    }

    [TestCase]
    public static void SlimBagTest()
    {
        UnmanagedColoredActiveList<Entry> ucl = new UnmanagedColoredActiveList<Entry>(64_000);

        var handle0 = ucl.Allocate(0, true, false);
        handle0.Data.EntryColor = 0;

        var handle1 = ucl.Allocate(0, true, false);
        handle1.Data.EntryColor = 1;

        var handle2 = ucl.Allocate(0, true, false);
        handle2.Data.EntryColor = 2;

        var handle3 = ucl.Allocate(1, true, false);
        handle3.Data.EntryColor = 3;

        var handle4 = ucl.Allocate(2, true, false);
        handle4.Data.EntryColor = 4;

        var handle5 = ucl.Allocate(3, true, false);
        handle5.Data.EntryColor = 7;

        ucl.MoveToInactive(handle5);
        ucl.MoveToInactive(handle5);

        ref var bla = ref ucl.Active(2)[0];
        var recHandle = ucl.GetHandle(ref bla);
        int wtf = recHandle.Data.EntryColor;

        var a = ucl.Active(0);
        var b = ucl.Active(1);
        var c = ucl.Active(2);
        var d = ucl.Active(3);
        var e = ucl.Active(4);
    }

}