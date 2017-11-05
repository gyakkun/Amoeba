using System;
using System.Collections.Generic;
using System.Linq;
using Amoeba.Service;
using Omnius.Base;
using Xunit;
using Xunit.Abstractions;

namespace Amoeba.Tests
{
    [Trait("Category", "Amoeba.Service")]
    public class RouteTableTests : TestsBase
    {
        private readonly Random _random = new Random();

        public RouteTableTests(ITestOutputHelper output) : base(output)
        {

        }

        [Fact]
        public void SearchTest()
        {
            var tempList = new List<Node<int>>();

            for (int i = 0; i < 256; i++)
            {
                var node = new Node<int>(_random.GetBytes(32), i);
                tempList.Add(node);
            }

            var sortedList = RouteTable<int>.Search(new byte[32], null, tempList.Randomize(), 32).ToList();

            tempList.Sort((x, y) => Unsafe.Compare(x.Id, y.Id));

            Assert.True(CollectionUtils.Equals(sortedList.Select(n => n.Value), tempList.Select(n => n.Value).Take(32)));
        }
    }
}
