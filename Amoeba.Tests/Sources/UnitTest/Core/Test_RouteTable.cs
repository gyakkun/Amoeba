using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amoeba.Core;
using Omnius.Base;
using Xunit;

namespace Amoeba.Tests
{
    public class Test_RouteTable : TestSetupBase
    {
        private readonly Random _random = new Random();

        [Fact]
        public void Test_Search()
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
