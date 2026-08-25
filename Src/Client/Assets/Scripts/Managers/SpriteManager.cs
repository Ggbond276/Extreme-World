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
        public Sprite[] classSprites;

        [Header("在线状态")]
        public Sprite[] onlineStatusSprites;


        /// <summary>
        /// 传入职业枚举 获取职业图标
        /// </summary>
        /// <param name="characterClass"></param>
        /// <returns></returns>
        /// <summary>
        /// 传入职业枚举 获取职业图标
        /// </summary>
        public Sprite GetClassSprite(CharacterClass characterClass)
        {
            int index = (int)characterClass - 1;

            // 防御：防止拿到无职业(0)变成-1导致越界，同时防止数组没配置
            if (classSprites != null && index >= 0 && index < classSprites.Length)
            {
                return this.classSprites[index];
            }

            // 如果越界了或者没职业，直接返回 null，让 UI 保持空白也比让游戏崩溃强！
            return null;
        }

        /// <summary>
        /// 根据在线状态获取对应的图标 (true 为在线，false 为离线)
        /// </summary>
        public Sprite GetStatusSprite(bool isOnline)
        {
            // 如果在线返回下标 1 的图片，离线返回下标 0 的图片
            int index = isOnline ? 1 : 0;

            // 防御性编程：防止数组越界或未赋值
            if (onlineStatusSprites != null && index < onlineStatusSprites.Length)
            {
                return onlineStatusSprites[index];
            }
            return null;
        }
    }
}
