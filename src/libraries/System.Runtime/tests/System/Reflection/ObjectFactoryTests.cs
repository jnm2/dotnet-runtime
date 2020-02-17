// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace System.Reflection.Tests
{
    public class ObjectFactoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(AbstractClass))]
        [InlineData(typeof(ClassWithoutParameterlessCtor))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(ReadOnlySpan<byte>))]
        [InlineData(typeof(string))]
        [InlineData(typeof(byte[]))]
        [InlineData(typeof(Array))]
        [InlineData(typeof(IInterface))]
        public void CreateFactory_FailureCases(Type type)
        {
            try
            {
                var factory = ObjectFactory.CreateFactory(type);
            }
            catch
            {
                return; // success!
            }

            throw new XunitException("Didn't expect ObjectFactory ctor to succeed.");
        }

        [Fact]
        public void CreateFactory_FailureCases_More()
        {
            CreateFactory_FailureCases(typeof(int).MakeByRefType());
            CreateFactory_FailureCases(typeof(List<>));
            CreateFactory_FailureCases(typeof(List<>).GetGenericArguments()[0]);

            Type canonType = typeof(object).Assembly.GetType("System.__Canon");

            CreateFactory_FailureCases(canonType);
            CreateFactory_FailureCases(typeof(List<>).MakeGenericType(canonType));
        }

        [Fact]
        public void FactoryTests()
        {
            RunTests<object>();
            RunTests<int>(i => Assert.Equal(0, i));
            RunTests<int?>();
            RunTests<SampleClass>(obj => Assert.True(obj.HasInstanceCtorRun));
        }

        private static void RunTests<T>(Action<T> verifier = null)
        {
            bool isNullableOfT = typeof(T).IsValueType && default(T) == null;

            ObjectFactory factory1 = ObjectFactory.CreateFactory(typeof(T));
            Assert.Equal(typeof(T), factory1.TargetType);
            object retVal1 = factory1.CreateInstance();

            if (isNullableOfT)
            {
                Assert.Null(retVal1);
            }
            else
            {
                Assert.IsType<T>(retVal1);
                verifier?.Invoke((T)retVal1);
            }

            ObjectFactory<T> factory2 = ObjectFactory.CreateFactory<T>();
            Assert.Equal(typeof(T), factory2.TargetType);
            T retVal2 = factory2.CreateInstance();

            if (isNullableOfT)
            {
                Assert.Null(retVal2);
            }
            else
            {
                Assert.IsType<T>(retVal2);
                verifier?.Invoke(retVal2);
            }
        }

        public class SampleClass
        {
            public bool HasInstanceCtorRun = true;
        }

        public abstract class AbstractClass { }

        public class ClassWithoutParameterlessCtor
        {
            public ClassWithoutParameterlessCtor(int unused) { }
        }

        public interface IInterface { }
    }
}
