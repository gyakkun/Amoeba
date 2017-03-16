using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amoeba.Core;
using NUnit.Framework;
using Omnius.Base;

namespace Amoeba.Test
{
    [TestFixture(Category = "Amoeba.Core.RouteTable<T>")]
    class Test_RouteTable
    {
        private readonly Random _random = new Random();

        [Test]
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

            Assert.IsTrue(CollectionUtils.Equals(sortedList.Select(n => n.Value), tempList.Select(n => n.Value).Take(32)));
        }
    }
}
