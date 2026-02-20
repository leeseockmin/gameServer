using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Common
{
	public enum ErrrorCode
	{
		NONE = 0,
		SUCCESS = 1,
		CREATE_USER,
		UNKNOW_ERROR,
		INVAILD_PACKET_INFO,
		INVAILD_USER_TOKEN,
		INVAILD_NICK_NAME,

		// 매칭
		ALREADY_IN_MATCH_QUEUE,
		NOT_IN_MATCH_QUEUE,
		ALREADY_IN_GAME,

		// 게임 룸
		ROOM_NOT_FOUND,
		NOT_IN_ROOM,
		GAME_NOT_STARTED,
		GAME_ALREADY_ENDED,
	}

	/// <summary>
	/// ErrrorCode의 오타를 수정한 올바른 이름 — 기존 파일 참조 호환을 위해 별칭으로 유지.
	/// </summary>
	public enum ErrorCode
	{
		NONE = 0,
		SUCCESS = 1,
		CREATE_USER,
		UNKNOW_ERROR,
		INVAILD_PACKET_INFO,
		INVAILD_USER_TOKEN,
		INVAILD_NICK_NAME,

		// 매칭
		ALREADY_IN_MATCH_QUEUE,
		NOT_IN_MATCH_QUEUE,
		ALREADY_IN_GAME,

		// 게임 룸
		ROOM_NOT_FOUND,
		NOT_IN_ROOM,
		GAME_NOT_STARTED,
		GAME_ALREADY_ENDED,
	}
}
