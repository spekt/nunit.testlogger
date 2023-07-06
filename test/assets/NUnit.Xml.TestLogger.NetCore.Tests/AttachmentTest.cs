using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.Xml.TestLogger.NetFull.Tests
{
    [TestFixture]
    public class AttachmentTest
    {
        [Test]
        public void TestAddAttachment()
        {
            var file = Path.Combine(Path.GetTempPath(), "x.txt");
            if (!File.Exists(file))
            {
                File.Create(file);
            }
            TestContext.AddTestAttachment(file, "description");
        }
    }
}