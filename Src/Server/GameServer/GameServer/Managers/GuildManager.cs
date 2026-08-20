using Common;
using GameServer.Entities;
using GameServer.Manager;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class GuildManager : Singleton<GuildManager>
    {

        public Dictionary<int, Guild> Guilds = new Dictionary<int, Guild>();
        /// <summary>
        /// Key : CharacterId Value : GuildId
        /// </summary>
        public Dictionary<int, int> CharacterGuildIdMap = new Dictionary<int, int>();

        /// <summary>
        /// 初始化内存数据方法
        /// </summary>
        public void Init()
        {
            var allTGuilds = DBService.Instance.Entities.TGuildSet.ToList();
            var allTGuildMembers = DBService.Instance.Entities.TGuildMemberSet.ToList();
            var allTGuildApplies = DBService.Instance.Entities.TGuildApplySet.ToList();

            foreach(var member in allTGuildMembers)
            {
                this.CharacterGuildIdMap[member.CharacterID] = member.TGuildId;
            }

            var memberByGuild = allTGuildMembers.GroupBy(m => m.TGuildId).ToDictionary(g => g.Key, g => g.ToList());
            var applyByGuild = allTGuildApplies.GroupBy(m => m.TGuildId).ToDictionary(g => g.Key, g => g.ToList());


            foreach (TGuild tGuild in allTGuilds)
            {
                Guild guild = new Guild(tGuild);

                int guildId = guild.Data.Id;

                memberByGuild.TryGetValue(guildId, out List<TGuildMember> tGuildMembers);
                applyByGuild.TryGetValue(guildId, out List<TGuildApply> tGuildApply);

                guild.InitGuild(tGuildMembers, tGuildApply);
            }
        }

        /// <summary>
        /// 获取全服公会列表的网络数据
        /// </summary>
        /// <returns></returns>
        public List<NGuildInfo> GetGuildsInfo()
        {
            List<NGuildInfo> result = new List<NGuildInfo>();
            
            foreach(Guild guild in this.Guilds.Values) 
            {
                result.Add(guild.ToNGuildInfo());
            }

            return result;
        }

        /// <summary>
        /// 用guildId获取公会实体
        /// </summary>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public Guild GetGuild(int guildId)
        {
            if(this.Guilds.TryGetValue(guildId, out Guild guild))
            {
                return guild;
            }
            return null;
        }

        /// <summary>
        /// 根据用户的ID查询所在的公会
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public int GetGuildIdByCharacter(int characterId) {
            if (this.CharacterGuildIdMap.TryGetValue(characterId, out int guildId))
            {
                return guildId;
            }
            return 0; // 没查到就是没公会
        }
        /// <summary>
        /// 离开公会 (主动退出)
        /// 清理数据库成员映射、内存公会字典以及在线角色的内存状态
        /// </summary>
        /// <param name="guildId">公会ID</param>
        /// <param name="characterId">角色ID</param>
        /// <returns>退出是否成功</returns>
        internal bool LeaveGuild(int guildId, int characterId)
        {
            Guild guild = this.GetGuild(guildId);
            GuildMember guildMember = guild.GetGuildMember(characterId);
            Character onlineCharacter = CharacterManager.Instance.GetCharacter(characterId);
            if (guild == null) 
                return false;

            try
            {
                var dbMember = DBService.Instance.Entities.TGuildMemberSet.FirstOrDefault(m => m.TGuildId == guildId && m.CharacterID == characterId);
                if(dbMember != null)
                {
                    DBService.Instance.Entities.TGuildMemberSet.Remove(dbMember);
                }

                DBService.Instance.save();


                bool success = guild.RemoveGuildMember(characterId);
                if (!success) 
                    return false;



                // 因为有的时候玩家会有掉线的情况 掉线就不用修改内存数据了
                if (onlineCharacter != null) 
                    onlineCharacter.GuildId = 0;

                // 从映射字典中抹除
                if(this.CharacterGuildIdMap.ContainsKey(characterId)) 
                    this.CharacterGuildIdMap.Remove(characterId);

                return true;
                

            }
            catch (Exception ex)
            {
                // 如果数据库挂了，或者字段不匹配，抓取异常并报错，防止服务器直接崩掉
                Log.Error($"LeaveGuild 数据库执行异常: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 创建公会
        /// 双表落库 (公会表与成员表)，并初始化内存大本营数据
        /// </summary>
        /// <param name="guildName">公会名称</param>
        /// <param name="notice">公会宗旨</param>
        /// <param name="reqLevel">入会等级限制</param>
        /// <param name="character">创建者(会长)对象</param>
        /// <returns>创建成功的公会内存对象，失败返回 null</returns>
        internal Guild CreateGuild(string guildName, string notice, int reqLevel, Character character)
        {
            DateTime now = DateTime.Now;

            try
            {
                var dbGuild = new TGuild()
                {
                    Name = guildName,
                    Level = 0,
                    LeaderID = character.Data.ID,
                    Notice = notice,
                    ReqLevel = reqLevel,
                    CreateTime = now
                };

                DBService.Instance.Entities.TGuildSet.Add(dbGuild);
                DBService.Instance.save();

                var dbMember = new TGuildMember()
                {
                    CharacterID = character.Data.ID,
                    Position = (int)GuildPosition.GuildPositionLeader,
                    JoinTime = now,
                    TGuildId = dbGuild.Id,
                };

                DBService.Instance.Entities.TGuildMemberSet.Add(dbMember);
                DBService.Instance.save();

                // 将新公会加入到字典
                Guild newguild = new Guild(dbGuild);
                this.Guilds[newguild.Data.Id] = newguild;

                this.CharacterGuildIdMap[character.Data.ID] = newguild.Data.Id; 


                // 将成员添加到公会的成员列表
                GuildMember newGuildMember = new GuildMember(dbMember);
                newguild.AddMember(newGuildMember);

                character.GuildId = newguild.Data.Id;

                return newguild;

            }
            catch (Exception ex)
            {

                Log.Error($"CreateGuild 数据库执行异常: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 解散公会
        /// 级联清理所有公会成员、申请列表及公会本体，并踢出所有在线成员
        /// </summary>
        /// <param name="guildId">要解散的公会ID</param>
        /// <param name="characterId">执行操作的会长角色ID</param>
        /// <returns>解散是否成功</returns>
        internal bool DisbandGuild(int guildId, int characterId)
        {
            Guild guild = this.GetGuild(guildId);
            if (guild == null)
                return false;

            try
            {
                // 查询出跟公会相关的所有成员字段列表
                var dbMembers = DBService.Instance.Entities.TGuildMemberSet.Where(m => m.TGuildId == guildId).ToList();
                var dbApplies = DBService.Instance.Entities.TGuildApplySet.Where(m => m.TGuildId == guildId).ToList();
                var dbGuild = DBService.Instance.Entities.TGuildSet.FirstOrDefault(m => m.Id == guildId);
                // 删除与公会相关的所有字段
                if (dbMembers.Count > 0)
                    DBService.Instance.Entities.TGuildMemberSet.RemoveRange(dbMembers);
                if (dbApplies.Count > 0)
                    DBService.Instance.Entities.TGuildApplySet.RemoveRange(dbApplies);
                if (dbGuild != null)
                    DBService.Instance.Entities.TGuildSet.Remove(dbGuild);

                DBService.Instance.save();

                foreach (var memberId in guild.Members.Keys )
                {
                    this.CharacterGuildIdMap.Remove(memberId);

                    Character onlineCharacter = CharacterManager.Instance.GetCharacter(memberId);
                    if (onlineCharacter != null)
                        onlineCharacter.GuildId = 0;
                }

                this.Guilds.Remove(guildId);
                return true;

            }
            catch (Exception ex)
            {
                Log.Error($"DisbandGuild 数据库执行异常: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 修改公会设置 (入会条件与宗旨)
        /// </summary>
        /// <param name="guildId">公会ID</param>
        /// <param name="newNotice">新宗旨 (传空表示不修改)</param>
        /// <param name="newReqLevel">新等级限制 (-1表示不修改)</param>
        /// <returns>修改是否成功</returns>
        internal bool ModifyGuildSettings(int guildId, string newNotice, int newReqLevel)
        {
            Guild guild = this.GetGuild(guildId);
            if (guild == null)
                return false;

            try
            {
                var dbGuild = DBService.Instance.Entities.TGuildSet.FirstOrDefault(m => m.Id == guildId);
                if (dbGuild == null)
                    return false;

                if (!string.IsNullOrEmpty(newNotice))
                    dbGuild.Notice = newNotice;
                if (newReqLevel != -1)
                    dbGuild.ReqLevel = newReqLevel;
                DBService.Instance.save();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"ModifyGuildSettings 数据库执行异常: {ex.Message}");
                return false;
                throw;
            }
        }
        /// <summary>
        /// 有人发送入会申请
        /// </summary>
        /// <param name="targetGuildId"></param>
        /// <param name="characterId"></param>
        /// <returns></returns>
        internal bool ApplyGuild(int targetGuildId, int characterId)
        {
            Guild guild = this.GetGuild(targetGuildId);
            if (guild == null)
                return false;

            if (guild.Applies.ContainsKey(characterId))
                return false;

            try
            {
                var dbApply = new TGuildApply();
                dbApply.TGuildId = targetGuildId;
                dbApply.CharacterID = characterId;
                dbApply.ApplyTime = DateTime.Now;

                DBService.Instance.Entities.TGuildApplySet.Add(dbApply);
                DBService.Instance.save();

                GuildApply newApply = new GuildApply(dbApply);
                guild.Applies[characterId] = newApply;

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"ApplyGuild 数据库事务执行异常: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 对入会申请进行处理
        /// </summary>
        /// <param name="guildId"></param>
        /// <param name="applicantId"></param>
        /// <param name="isAccept"></param>
        /// <returns></returns>
        internal bool ProcessApply(int guildId, int applicantId, bool isAccept)
        {
            Guild guild = this.GetGuild(guildId);
            if (guild == null)
                return false;

            try
            {
                // 找到并删除申请
                var dbApply = DBService.Instance.Entities.TGuildApplySet.FirstOrDefault(a => a.TGuildId == guildId && a.CharacterID == applicantId);
                if(dbApply == null)
                {
                    return false;
                }

                DBService.Instance.Entities.TGuildApplySet.Remove(dbApply);

                // 找到并删除成员
                TGuildMember dbMember = null;
                if(isAccept)
                {
                    dbMember = new TGuildMember();
                    dbMember.TGuildId = guildId;
                    dbMember.CharacterID = applicantId;
                    dbMember.Position = (int)GuildPosition.GuildPositionMember;
                    dbMember.JoinTime = DateTime.Now;
                    DBService.Instance.Entities.TGuildMemberSet.Add(dbMember);
                }

                DBService.Instance.save();

                if(guild.Applies.ContainsKey(applicantId))
                {
                    guild.Applies.Remove(applicantId);
                }

                if(isAccept && dbMember != null)
                {
                    GuildMember newGuildMember = new GuildMember(dbMember);
                    guild.AddMember(newGuildMember);

                    this.CharacterGuildIdMap[applicantId] = guildId;

                    Character onlineCharacter = CharacterManager.Instance.GetCharacter(applicantId);
                    if(onlineCharacter != null)
                    {
                        onlineCharacter.GuildId = guildId;
                    }
                   
                }

                return true;
            }
            catch (Exception ex)
            {
                // 事务一旦失败，EF 自动丢弃 pending 状态，数据库无任何残留
                // 我们的 catch 直接返回 false，告诉 Service 层失败，保证内存字典也不会被错误污染
                Log.Error($"ProcessApply 数据库事务执行异常: {ex.Message}");
                return false;
            }
        }

        internal bool ExcuteAdminCommand(int guildId, int operatorId, int targetId, GuildAdminCommand command)
        {
            Guild guild = this.GetGuild(guildId);
            if (guild == null) return false;

            GuildMember opMember = guild.GetGuildMember(operatorId);
            GuildMember targetMember = guild.GetGuildMember(targetId);

            if (opMember == null || targetMember == null || operatorId == targetId)
                return false;

            try
            {
                var dbTarget = DBService.Instance.Entities.TGuildMemberSet.FirstOrDefault(m => m.TGuildId == guildId && m.CharacterID == targetId);
                var dbOp = DBService.Instance.Entities.TGuildMemberSet.FirstOrDefault(m => m.TGuildId == guildId && m.CharacterID == operatorId);
                var dbGuild = DBService.Instance.Entities.TGuildSet.FirstOrDefault(g => g.Id == guildId);

                if (dbTarget == null || dbOp == null || dbGuild == null) return false;

                switch (command)
                {
                    case GuildAdminCommand.CommandKickMember:
                        if (opMember.Data.Position >= targetMember.Data.Position) return false;
                        DBService.Instance.Entities.TGuildMemberSet.Remove(dbTarget);
                        break;
                    case GuildAdminCommand.CommandPromoteVice:
                        if (opMember.Data.Position != (int)GuildPosition.GuildPositionLeader) return false;
                        if (targetMember.Data.Position != (int)GuildPosition.GuildPositionMember) return false;
                        dbTarget.Position = (int)GuildPosition.GuildPositionViceLeader;
                        break;
                    case GuildAdminCommand.CommandDemoteNormal:
                        if (opMember.Data.Position != (int)GuildPosition.GuildPositionLeader) return false;
                        if (targetMember.Data.Position != (int)GuildPosition.GuildPositionViceLeader) return false;
                        dbTarget.Position = (int)GuildPosition.GuildPositionMember;
                        break;
                    case GuildAdminCommand.CommandTransferLeader:
                        if (opMember.Data.Position != (int)GuildPosition.GuildPositionLeader) return false;
                        dbTarget.Position = (int)GuildPosition.GuildPositionLeader;
                        dbOp.Position = (int)GuildPosition.GuildPositionMember;
                        dbGuild.LeaderID = targetId;
                        break;
                    default:
                        return false;
                }

                DBService.Instance.save();

                if(command == GuildAdminCommand.CommandKickMember)
                {
                    guild.RemoveGuildMember(targetId);
                    this.CharacterGuildIdMap.Remove(targetId);
                    Character onlineTarget = CharacterManager.Instance.GetCharacter(targetId);
                    if (onlineTarget != null) onlineTarget.GuildId = 0;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"ExecuteAdminCommand 数据库事务执行异常: {ex.Message}");
                throw;
            }
        }
    }
}
