using Common;
using GameServer.Entities;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    /// <summary>
    /// 这个是管理所有组队信息的管理器
    /// </summary>
    class TeamManager : Singleton<TeamManager>
    {
        /// <summary>
        /// 用于全量遍历
        /// </summary>
        List<Team> teamList = new List<Team>();
        /// <summary>
        /// 用于精准查找
        /// </summary>
        Dictionary<int, Team> teamDict = new Dictionary<int, Team>();
        /// <summary>
        /// 回收对象池
        /// </summary>
        Queue<Team> teamPool = new Queue<Team>();


        /// <summary>
        /// 建立一个队伍赋值给请求者
        /// </summary>
        /// <param name="leader"></param>
        public Team CreateTeam(Character leader)
        {
            Team team;
            if(this.teamPool.Count != 0)
            {
                team = this.teamPool.Dequeue();
            } else
            {
                team = new Team();
            }

            team.Init(leader);
            leader.team = team;

            this.teamList.Add(team);
            this.teamDict[team.Id] = team;

            return team;
        }

        /// <summary>
        /// 组员离开队伍的方法
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="characterId"></param>
        public bool LeaveTeam(int teamId, int characterId, out string errorMsg)
        {

            errorMsg = string.Empty;

            if(!teamDict.ContainsKey(teamId))
            {
                errorMsg = "队伍Id错误";
                return false;
            }

            Team team = teamDict[teamId];
            Character member = team.GetMember(characterId);
            if ( member == null)
            {
                errorMsg = "请求者Id错误";
                return false;
            }

            if(team.Leader == member)
            {
                teamList.Remove(team);
                teamDict.Remove(team.Id);
                team.Clear();
                teamPool.Enqueue(team);
                return true;
            } else
            {
                team.RemoveMember(member);
                return true;
            }
        }

    }
}
