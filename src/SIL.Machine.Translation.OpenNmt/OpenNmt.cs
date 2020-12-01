using System;
using Python.Runtime;

namespace SIL.Machine.Translation.OpenNmt
{
	internal static class OpenNmt
	{
		private static readonly Lazy<dynamic> LazyModule = new Lazy<dynamic>(() => ImportModule("opennmt"));

		public static dynamic Module => LazyModule.Value;

		private static dynamic ImportModule(string moduleName)
		{
			Init();
			using (Py.GIL())
			{
				return Py.Import(moduleName);
			}
		}

		private static void Init()
		{
			using (Py.GIL())
			using (PyScope scope = Py.CreateScope())
			{
				var action = new Action<dynamic>(HandleLogRecord);
				scope.Set("handler_action", action.ToPython());
				scope.Exec(@"
import os
import logging

class ActionHandler(logging.Handler):
	def __init__(self, action):
		super().__init__()
		self.action = action

	def emit(self, record):
		self.action(record)

os.environ[""TF_CPP_MIN_LOG_LEVEL""] = ""3""
logging.basicConfig(level=logging.INFO, handlers=[ActionHandler(handler_action)])
");
			}
		}

		private static void HandleLogRecord(dynamic record)
		{
			using (Py.GIL())
				Console.WriteLine(record.name);
		}
	}
}
