using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Models
{
    class Friend
    {
        //public int Id { get; set; }
        //public int FriendID { get; set; }
        //public string FriendName { get; set; }
        //public int Class { get; set; }
        //public int Level { get; set; }
        //public int TCharacterID { get; set; }
        //public virtual TCharacter Onwer { get; set; }
        public TCharacterFriend DbFriend { get; private set; }

        // 通过语法糖映射底层数据 暴露给上层属性
        public int FriendId => DbFriend.FriendID;
        public string FriendName => DbFriend.FriendName;
        public int Class => DbFriend.Class;
        public int Level => DbFriend.Level;
        public bool isOnline { get; set; }
        public Friend(TCharacterFriend dbFriend)
        {
            this.DbFriend = dbFriend;
        }

        // 转换成网络数据
        //message NFriendInfo
        //{
        //    int32 id = 1;
        //    NCharacterInfo friendInfo = 2;
        //    int32 status = 3;
        //}

        public NFriendInfo ToNFriendInfo()
        {
            NFriendInfo friendInfo = new NFriendInfo();
            friendInfo.Id = this.FriendId;
            NCharacterInfo characterInfo = new NCharacterInfo();
            characterInfo.Name = this.FriendName;
            characterInfo.Class = (CharacterClass)this.Class;
            characterInfo.Level = this.Level;
            friendInfo.friendInfo = characterInfo;
            friendInfo.Status = this.isOnline ? 1 : 0;

            return friendInfo;
            
        }
    }
}
