namespace Installer.Tests
{
    public class TestsBase
    {
        protected byte[] BuildTestData(int size)
        {
            var data = new byte[size];
            for (var x = 0; x < data.Length; x++)
            {
                data[x] = (byte)(DateTime.Now.Ticks % 255);
            }

            return data;
        }
    }
}
