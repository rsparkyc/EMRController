using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EMRController.Utils
{
	static class EMRUtils
	{
		private const string logName = "EMR";
		public static void Log(params object[] message)
		{
			Log(Array.ConvertAll(message, item => item.ToString()));
		}

		public static void Log(params string[] message)
		{
			var builder = StringBuilderCache.Acquire();
			builder.Append("[").Append(logName).Append("] ");
			builder.Append(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff")).Append(" - ");
			foreach (string part in message) {
				builder.Append(part);
			}
			UnityEngine.Debug.Log(builder.ToStringAndRelease());
		}
	}
}
