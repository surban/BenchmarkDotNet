﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class XUnitNameProviderTests
    {
        private void AssertBenchmarkName<T>(string expectedBenchmarkName)
        {
            var benchmark = BenchmarkConverter.TypeToBenchmarks(typeof(T)).Benchmarks.Single();

            Assert.Equal(expectedBenchmarkName, XUnitNameProvider.GetBenchmarkName(benchmark));
        }

        [Fact]
        public void MethodsWithoutArgumentsAreSupported() 
            => AssertBenchmarkName<SimplestCase>("BenchmarkDotNet.Tests.SimplestCase.Method");

        [Fact]
        public void NestedTypesAreSupported()
        {
            AssertBenchmarkName<Level0.Level1>("BenchmarkDotNet.Tests.Level0+Level1.Method"); // '+' is used for nested types 
            AssertBenchmarkName<Level0.Level1.Level2>("BenchmarkDotNet.Tests.Level0+Level1+Level2.Method"); // '+' is used for nested types 
        } 

        [Fact]
        public void IntegerArgumentsAreSupported() 
            => AssertBenchmarkName<SingleIntArgument>("BenchmarkDotNet.Tests.SingleIntArgument.Method(arg: 1)");

        [Fact]
        public void CharacterArgumentsAreSupported() 
            => AssertBenchmarkName<SingleCharArgument>("BenchmarkDotNet.Tests.SingleCharArgument.Method(arg: 'c')");

        [Fact]
        public void NullArgumentsAreSupported() 
            => AssertBenchmarkName<SingleNullArgument>("BenchmarkDotNet.Tests.SingleNullArgument.Method(arg: null)"); // null is just a null (not "null")

        [Fact]
        public void EnumArgumentsAreSupported() 
            => AssertBenchmarkName<SingleEnumArgument>("BenchmarkDotNet.Tests.SingleEnumArgument.Method(arg: Read)"); // no enum type name, just value

        [Fact]
        public void MultipleArgumentsAreSupported() 
            => AssertBenchmarkName<FewStringArguments>("BenchmarkDotNet.Tests.FewStringArguments.Method(arg1: \"a\", arg2: \"b\", arg3: \"c\", arg4: \"d\")");

        [Fact]
        public void DateTimeArgumentsAreSupported() 
            => AssertBenchmarkName<SingleDateTimeArgument>("BenchmarkDotNet.Tests.SingleDateTimeArgument.Method(arg: 9999-12-31T23:59:59.9999999)");

        [Fact]
        public void GuidArgumentsAreSupported() 
            => AssertBenchmarkName<SingleGuidArgument>("BenchmarkDotNet.Tests.SingleGuidArgument.Method(arg: 00000000-0000-0000-0000-000000000000)");

        [Fact]
        public void GenericArgumentsAreSupported() 
            => AssertBenchmarkName<SimpleGeneric<int>>("BenchmarkDotNet.Tests.SimpleGeneric<Int32>.Method");

        [Fact]
        public void ArraysAreSupported() 
            => AssertBenchmarkName<WithArray>("BenchmarkDotNet.Tests.WithArray.Method(array: [1, 2, 3], value: 4)");

        [Fact]
        public void UnicodeIsSupported() 
            => AssertBenchmarkName<WithCrazyUnicodeCharacters>("BenchmarkDotNet.Tests.WithCrazyUnicodeCharacters.Method(arg1: \"" + "FOO" + "\", arg2: \""+ "\u03C3" + "\", arg3: \"" + "x\u0305" + "\")");

        [Fact]
        public void VeryLongArraysAreSupported()
            => AssertBenchmarkName<WithBigArray>("BenchmarkDotNet.Tests.WithBigArray.Method(array: [0, 1, 2, 3, 4, ...])");
    }

    public class Level0
    {
        public class Level1
        {
            [Benchmark]
            public void Method() { }

            public class Level2
            {
                [Benchmark]
                public void Method() { }
            }
        }
    }

    public class SimplestCase
    {
        [Benchmark]
        public void Method() { }
    }

    public class SingleIntArgument
    {
        [Benchmark]
        [Arguments(1)]
        public void Method(int arg) { }
    }

    public class SingleNullArgument
    {
        [Benchmark]
        [Arguments(null)]
        public void Method(object arg) { }
    }

    public class SingleEnumArgument
    {
        [Benchmark]
        [Arguments(FileAccess.Read)]
        public void Method(FileAccess arg) { }
    }

    public class SingleCharArgument
    {
        [Benchmark]
        [Arguments('c')]
        public void Method(char arg) { }
    }

    public class FewStringArguments
    {
        [Benchmark]
        [Arguments("a", "b", "c", "d")]
        public void Method(string arg1, string arg2, string arg3, string arg4) { }
    }

    public class SingleDateTimeArgument
    {
        [Benchmark]
        [ArgumentsSource(nameof(Date))]
        public void Method(DateTime arg) { }

        public IEnumerable<object[]> Date()
        {
            yield return new object[] { DateTime.MaxValue };
        }
    }

    public class SingleGuidArgument
    {
        [Benchmark]
        [ArgumentsSource(nameof(Guid))]
        public void Method(Guid arg) { }

        public IEnumerable<object[]> Guid()
        {
            yield return new object[] { System.Guid.Empty };
        }
    }

    public class WithArray
    {
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public void Method(int[] array, int value) { }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { ArrayParam<int>.ForPrimitives(new[] { 1, 2, 3 }), 4 };
        }
    }

    public class SimpleGeneric<T>
    {
        [Benchmark]
        public T Method() => default(T);
    }

    public class WithCrazyUnicodeCharacters
    {
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public void Method(string arg1, string arg2, string arg3) { }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { "FOO", "\u03C3", "x\u0305" }; // https://github.com/Microsoft/xunit-performance/blob/f1d1d62a934694d8cd19063e60e04c590711d904/tests/simpleharness/Program.cs#L29
        }
    }

    public class WithBigArray
    {
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public void Method(int[] array) { }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { ArrayParam<int>.ForPrimitives(Enumerable.Range(0, 100).ToArray()) };
        }
    }
}