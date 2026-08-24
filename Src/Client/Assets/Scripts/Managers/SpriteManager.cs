using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Managers
{
    class SpriteManager : MonoSingleton<SpriteManager>
    {
        [Header("职业图标")]
        public Sprite[] ClassSprites;

        [Header("在线状态")]
        public Sprite[] OnlineStatusSprites;


        /// <summary>
        /// 传入职业枚举 获取职业图标
        /// </summary>
        /// <param name="characterClass"></param>
        /// <returns></returns>
        public Sprite GetClassSprite(CharacterClass characterClass) {
            return this.ClassSprites[(int)characterClass - 1];
        }

        public Sprite GetClassSprite(int status)
        {
            return this.OnlineStatusSprites[status];
        }
    }
}
