using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
	public class Messenger : EventAggregator
	{
		public static Messenger Instance { get; } = new Messenger();
	}
}
