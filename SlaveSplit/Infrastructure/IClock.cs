using System;
namespace SlaveSplit.Infrastructure { public interface IClock { DateTime Now { get; } } public sealed class SystemClock : IClock { public DateTime Now { get { return DateTime.Now; } } } }
