using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Appveyor.TestLogger.Tests
{
    [TestClass]
    public class NetCoreTestClass
    {
        [TestMethod]
        public void NetCorePassMethod1()
        {
            Console.Error.Write("StdError output of PassMethod1");
            Console.Out.WriteLine("StdOut output of PassMethod1");
            Console.WriteLine("Console output of PassMethod1");
        }

        [TestMethod]
        public void NetCorePassMethod2()
        {
            Console.Error.Write("StdError output of PassMethod2");
            Console.Out.WriteLine("StdOut output of PassMethod2");
            Console.WriteLine("Console output of PassMethod2");
        }

        [TestMethod]
        public void NetCoreFailMethod1()
        {
            Console.Error.Write("StdError output of FailMethod1");
            Console.Out.WriteLine("StdOut output of FailMethod1");
            Console.WriteLine("Console output of FailMethod1");
            ThrowExceptionClass t = new ThrowExceptionClass();
            t.foo();
        }

        [TestMethod]
        public void NetCoreFailMethod2()
        {
            Console.Error.Write("StdError output of FailMethod2");
            Console.Out.WriteLine("StdOut output of FailMethod2");
            Console.WriteLine("Console output of FailMethod2");
            ThrowExceptionClass t = new ThrowExceptionClass();
            t.foo();
        }

        [TestMethod]
        [Ignore]
        public void NetCoreSkipTest1()
        {
        }

        [TestMethod]
        [Ignore]
        public void NetCoreSkipTest2()
        {
        }
    }

    public class ThrowExceptionClass
    {
        public void foo()
        {
            throw new Exception("Catch me if you can");
        }
    }
}
