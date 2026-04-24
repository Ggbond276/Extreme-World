using Common.Data;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    class Quest
    {
        public QuestDefine Define;
        public NQuestInfo Info;

        public Quest()
        {

        }

        public Quest(NQuestInfo info)
        {
            this.Info = info;
            this.Define = DataManager.Instance.Quests[info.QuestId];
        }
        public Quest(QuestDefine define)
        {
            this.Define = define;
        }

        public string GetTypeName()
        {
            return EnumUtil.GetEnumDescription(this.Define.Type);
        }
    }
}
