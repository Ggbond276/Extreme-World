using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using GameServer;
using GameServer.Entities;
using GameServer.Services;
using SkillBridge.Message;

namespace Network
{
    class NetSession : INetSession
    {
        public TUser User { get; set; }
        public Character Character { get; set; }
        public NEntity Entity { get; set; }

        // 我先用我的设计思路来处理一下
        // 我想在这里使用一个后处理器的容器
        // 然后提供一个注册方法 将你想要进行后处理的Manager 全都注册在这里
        //public List<IPostResponser> Responsers = new List<IPostResponser>();

        //public void ResgisterResponser(IPostResponser responser)
        //{
        //    if(!Responsers.Contains(responser))
        //        Responsers.Add(responser);

        //}

        internal void Disconnected()
        {
            if (Character != null)
            {
                Log.InfoFormat("CharacterLeave: {0}", Character.entityId);
                UserService.Instance.CharacterLeave(Character);
            }
        }


        // 这里的NetMessageRespnse 就像是一辆可以送货的大卡车
        NetMessage response;
        public  NetMessageResponse Response
        {
            get
            {
                if (response == null)
                    response = new NetMessage();
                if (response.Response == null)
                    response.Response = new NetMessageResponse();
                return response.Response;
            }
        }

        // 这个就是装货的过程 别人之前已经将货物装进了大卡车中 本来可以直接转字节流发送
        public byte[] GetResponse()
        {
            if (Response != null)
            {


                // 但是在这里 我们希望检查一下状态处理器 有没有更新的状态一起发送出去
                // 但是现在核心痛点来了 我们现在想要 另外一个频繁更新的状态也可以使用这个
                // 那我们就要修改源代码了 如果后面又questManager或者别的各种Manager
                // 涉及到频繁更新 都需要修改GetResponse的代码 这违背了开闭原则 我们希望
                // 我们不需要修改GetResponse就可以接入别的处理器
                // 1.如果当前角色存在 2.如果当前状态管理器中有修改的状态
                //if (this.Character != null && this.Character.statusManager.Status != null)
                //{
                //    // 3. 就将这些状态全部装进NetMessageResponse中打包
                //    // 我们现在将apply这个改成postResponse
                //    // 所有想要后处理的Manager都需要实现postResponse
                //    // 这里需要给你一个Response 才可以处理进卡车
                //    this.Character.statusManager.ApplyReponse(Response);
                //}

                // 既然我们现在有容器了 我们就只需要循环遍历去调用postResponser即可
                // 这样我们就将职责移交给了注册的Manager 但是现在我们需要考虑注册的时机是什么时候

                // 需要处理的两处隐患
                // 内存泄漏与重复注册： 如果玩家断线重连，或者退出到选人界面再进游戏，你有没有在 Disconnected() 里清空 Responsers 列表？如果没有，列表会越来越大，甚至包含上一个废弃角色的 Manager。
                // 线程安全： 如果网络发送在单独的网络线程，而 Manager 注册在主逻辑线程，foreach 遍历时如果有人调用了 ResgisterResponser，C# 会抛出 CollectionWasModified 异常。
                //foreach (IPostResponser responser in this.Responsers)
                //{
                //    responser.PostResponse(Response);
                //}

                if(this.Character != null)
                {
                    // 把车给你 自己去装货
                    Character.PostResponse(Response);
                }


                // 4. 转成字节流
                byte[] data = PackageHandler.PackMessage(response);
                // 5. 返回字节流数据
                response = null;
                return data;
            }
            return null;
        }
    }
}
