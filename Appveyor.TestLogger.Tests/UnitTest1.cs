using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Appveyor.TestLogger.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void PassMethod1()
        {
            Console.Error.Write("StdError output of PassMethod1");
            Console.Out.WriteLine("StdOut output of PassMethod1");
            Console.WriteLine("Console output of PassMethod1");
        }

        [TestMethod]
        public void PassMethod2()
        {
            Console.Error.Write("StdError output of PassMethod2");
            Console.Out.WriteLine("StdOut output of PassMethod2");
            Console.WriteLine("Console output of PassMethod2");
        }

        [TestMethod]
        public void FailMethod1()
        {
            Console.Error.Write("StdError output of FailMethod1");
            Console.Out.WriteLine("StdOut output of FailMethod1");
            Console.WriteLine("Console output of FailMethod1");
            ThrowExceptionClass t = new ThrowExceptionClass();
            t.foo();
        }

        [TestMethod]
        public void FailMethod2()
        {
            Console.Error.Write("StdError output of FailMethod2");
            Console.Out.WriteLine("StdOut output of FailMethod2");
            Console.WriteLine("Console output of FailMethod2");
            ThrowExceptionClass t = new ThrowExceptionClass();
            t.foo();
        }

        [TestMethod]
        [Ignore]
        public void SkipTest1()
        {
        }

        [TestMethod]
        [Ignore]
        public void SkipTest2()
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
