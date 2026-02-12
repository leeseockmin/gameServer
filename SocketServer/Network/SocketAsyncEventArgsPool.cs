using System.Collections.Concurrent;
using System.Net.Sockets;

namespace SocketServer.Network
{
	/// <summary>
	/// SocketAsyncEventArgs 오브젝트 풀
	/// GC 압력을 줄이기 위해 미리 할당하고 재사용
	/// </summary>
	public class SocketAsyncEventArgsPool
	{
		private readonly ConcurrentStack<SocketAsyncEventArgs> _pool;

		public int Count => _pool.Count;

		public SocketAsyncEventArgsPool(int capacity)
		{
			_pool = new ConcurrentStack<SocketAsyncEventArgs>();

			for (int i = 0; i < capacity; i++)
			{
				_pool.Push(CreateNew());
			}
		}

		private SocketAsyncEventArgs CreateNew()
		{
			return new SocketAsyncEventArgs();
		}

		public SocketAsyncEventArgs Pop()
		{
			if (_pool.TryPop(out var args))
				return args;

			return CreateNew();
		}

		public void Push(SocketAsyncEventArgs args)
		{
			args.AcceptSocket = null;
			args.UserToken = null;
			args.SetBuffer(null, 0, 0);
			_pool.Push(args);
		}
	}
}
