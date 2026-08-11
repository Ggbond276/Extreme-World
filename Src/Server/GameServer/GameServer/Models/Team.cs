using GameServer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;

namespace GameServer.Models
{
    class Team
    {
        /// <summary>
        /// 自增器，自增Team的Id
        /// </summary>
        public static int count = 0;
        /// <summary>
        /// 队伍Id
        /// </summary>
        public int Id;
        /// <summary>
        /// 队伍队长
        /// </summary>
        public Character Leader;
        /// <summary>
        /// 队伍的队员信息
        /// </summary>
        public List<Character> Members = new List<Character>();
        /// <summary>
        /// 组队最后更新的时间戳
        /// </summary>
        public float timestamp = 0f;


        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="leader"></param>
        public Team()
        {
        }
        /// <summary>
        /// 队伍信息初始化方法
        /// </summary>
        public void Init(Character leader)
        {

            this.Id = System.Threading.Interlocked.Increment(ref count);

            this.Leader = leader;
            this.Members.Clear();
            this.Members.Add(leader);

            this.timestamp = Time.time;
        }
        /// <summary>
        /// 加入成员
        /// </summary>
        /// <param name="character"></param>
        public void AddMember(Character character)
        {
            this.Members.Add(character);

            this.timestamp = Time.time;
        }
        /// <summary>
        ///  删除成员
        /// </summary>
        /// <param name="character"></param>
        public void RemoveMember(Character character)
        {
            character.team = null;
            this.Members.Remove(character);
            this.timestamp = Time.time;
        }
        /// <summary>
        /// 设置队长
        /// </summary>
        /// <param name="newLeader"></param>
        public void SetLeader(Character newLeader)
        {
            this.Leader = newLeader;

            this.timestamp = Time.time;
        }
        /// <summary>
        /// 将实体信息转换成网络信息的方法
        /// </summary>
        /// <returns></returns>
        internal NTeamInfo ToNTeamInfo()
        {
            NTeamInfo nTeamInfo = new NTeamInfo();
            nTeamInfo.Id = this.Id;
            nTeamInfo.Leader = this.Leader.Data.ID;
            foreach(Character cha in Members)
            {
                nTeamInfo.Members.Add(cha.Info);
            }
            return nTeamInfo;
        }
        /// <summary>
        /// 获取组员
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public Character GetMember(int characterId)
        {
            return this.Members.Find(c => c.Id == characterId);
        }
        /// <summary>
        /// 清理队伍
        /// </summary>
        public void Clear()
        {
            this.Id = -1;
            this.Leader = null;

            // 清理双向绑定
            foreach(Character cha in this.Members)
            {
                cha.team = null;
            }
            this.Members.Clear();

            this.timestamp = 0f;
        }
    }
}
