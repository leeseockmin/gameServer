using System.Net.Sockets;

namespace SocketServer.Network
{
	/// <summary>
	/// 대용량 바이트 배열을 하나 할당한 뒤 SocketAsyncEventArgs에 슬라이스를 배분
	/// LOH 단편화 방지
	/// </summary>
	public class BufferManager
	{
		private readonly byte[] _totalBuffer;
		private readonly int _bufferSize;
		private int _currentIndex;
		private readonly Stack<int> _freeIndexPool;

		public BufferManager(int totalBytes, int bufferSize)
		{
			_totalBuffer = new byte[totalBytes];
			_bufferSize = bufferSize;
			_currentIndex = 0;
			_freeIndexPool = new Stack<int>();
		}

		/// <summary>
		/// SocketAsyncEventArgs에 버퍼 슬라이스 할당
		/// </summary>
		public bool SetBuffer(SocketAsyncEventArgs args)
		{
			if (_freeIndexPool.Count > 0)
			{
				args.SetBuffer(_totalBuffer, _freeIndexPool.Pop(), _bufferSize);
				return true;
			}

			if (_currentIndex + _bufferSize > _totalBuffer.Length)
				return false;

			args.SetBuffer(_totalBuffer, _currentIndex, _bufferSize);
			_currentIndex += _bufferSize;
			return true;
		}

		/// <summary>
		/// 버퍼 슬라이스 반환
		/// </summary>
		public void FreeBuffer(SocketAsyncEventArgs args)
		{
			_freeIndexPool.Push(args.Offset);
			args.SetBuffer(null, 0, 0);
		}
	}
}
