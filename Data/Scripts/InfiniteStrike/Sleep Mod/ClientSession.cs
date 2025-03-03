using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace InfiniteStrike.SleepMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]

    public class ClientSession : MySessionComponentBase
    {

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            // check if we are running on a dedicated server, so that the whole crossplay thing will work, 
            // but if this is not needed, we will just start a server session.
            if (Common.isServer())
            {
                Common.WriteLog("Detected Dedicated Server, running in Server Mode.");
            }
            else
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Common.ModInfo.pMessageHandle, OnRecieveMessageFromServer);
                Common.WriteLog("Running on Client only mode.");
            };



            //base.Init(sessionComponent);
        }

        private void OnRecieveMessageFromServer(ushort arg1, byte[] arg2, ulong arg3, bool arg4)
        {
            byte[] messageData = arg2;
            OnRecieveMessageFromServer(MyAPIGateway.Utilities.SerializeFromBinary<string>(messageData));
        }

        private void OnRecieveMessageFromServer(object obj)
        {
            BedLogicComponent.playerMessage = obj.ToString();
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Common.ModInfo.pMessageHandle, OnRecieveMessageFromServer);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CryoChamber), true)]
    public class BedLogicComponent : MyGameLogicComponent
    {

        public static string playerMessage = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Common.BedChatDispacher.beds.Add(Entity as IMyCryoChamber);
        }

        public override void Close()
        {
            Common.BedChatDispacher.beds.Remove(Entity as IMyCryoChamber);
        }

        public override void UpdateBeforeSimulation10()
        {
            if (Entity != null && Entity is IMyCryoChamber)
            {
                IMyCryoChamber currentBed = Entity as IMyCryoChamber;
                if (playerMessage != null && Common.Utils.isCurrentBedValid(currentBed) && !Common.isServer())
                {
                    MyAPIGateway.Utilities.ShowNotification(playerMessage, 150);
                }
            }
        }
    }
}