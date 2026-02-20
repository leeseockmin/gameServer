using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Common
{
	public enum ErrorCode
	{
		NONE = 0,
		SUCCESS = 1,
		CREATE_USER,
		UNKNOW_ERROR,
		INVALID_PACKET_INFO,
		INVALID_USER_TOKEN,
		INVALID_NICK_NAME,

		// 매칭
		ALREADY_IN_MATCH_QUEUE,
		NOT_IN_MATCH_QUEUE,
		ALREADY_IN_GAME,

		// 게임 룸
		ROOM_NOT_FOUND,
		NOT_IN_ROOM,
		GAME_NOT_STARTED,
		GAME_ALREADY_ENDED,

		// 계정 연동
		ACCOUNT_LINK_ALREADY_LINKED    = 14,
		ACCOUNT_LINK_INVALID_KEY       = 15,
		ACCOUNT_LINK_NOT_FOUND         = 16,
		ACCOUNT_LINK_LAST_LINK         = 17,
		ACCOUNT_LINK_UNSUPPORTED_TYPE  = 18,
	}
}
