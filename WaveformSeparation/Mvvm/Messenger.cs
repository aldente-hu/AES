using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HirosakiUniversity.Aldente.AES.WaveformSeparation.Mvvm
{
	public class Messenger
	{
		static Messenger _instance = new Messenger();

		public static Messenger Default
		{
			get
			{
				return _instance;
			}
		}

		List<ActionInfo> list = new List<ActionInfo>();
		
		public void Register<TMessage>(ViewModelBase sender, Action<TMessage> action)
		{
			list.Add(new Mvvm.Messenger.ActionInfo
			{
				Type = typeof(TMessage),
				Sender = sender,
				Action = action
			});
		} 

		public void Send<TMessage>(ViewModelBase sender, TMessage message)
		{
			var query = list.Where(o => o.Sender == sender && o.Type == message.GetType()).Select(o => o.Action as Action<TMessage>);
			foreach (var action in query)
			{
				action(message);
			}
		}

		class ActionInfo
		{
			public Type Type { get; set; }
			public ViewModelBase Sender { get; set; }
			public Delegate Action { get; set; }
		}

	}

}
