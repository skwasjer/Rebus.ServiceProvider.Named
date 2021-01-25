using System;
using Rebus.Logging;
using Xunit.Abstractions;

namespace Rebus.ServiceProvider.Named
{
	public class RebusTestLoggerFactory : AbstractRebusLoggerFactory
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public RebusTestLoggerFactory(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}

		protected override ILog GetLogger(Type type)
		{
			return new RebusTestLogger(_testOutputHelper, RenderString);
		}

		public class RebusTestLogger : ILog
		{
			private readonly ITestOutputHelper _testOutputHelper;
			private readonly Func<string, object[], string> _renderString;

			public RebusTestLogger(ITestOutputHelper testOutputHelper, Func<string, object[], string> renderString)
			{
				_testOutputHelper = testOutputHelper;
				_renderString = renderString;
			}

			public void Debug(string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));

			}

			public void Info(string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));
			}

			public void Warn(string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));
			}

			public void Warn(Exception exception, string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));
				if (exception is { })
				{
					_testOutputHelper.WriteLine(exception.ToString());
				}
			}

			public void Error(string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));
			}

			public void Error(Exception exception, string message, params object[] objs)
			{
				_testOutputHelper.WriteLine(_renderString(message, objs));
				{
					_testOutputHelper.WriteLine(exception.ToString());
				}
			}
		}
	}
}
