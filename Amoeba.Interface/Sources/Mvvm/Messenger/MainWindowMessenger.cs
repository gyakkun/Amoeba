using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Interface
{
	public class MainWindowMessenger : EventAggregator
	{
		public static MainWindowMessenger ShowEvent { get; } = new MainWindowMessenger();
	}
}
