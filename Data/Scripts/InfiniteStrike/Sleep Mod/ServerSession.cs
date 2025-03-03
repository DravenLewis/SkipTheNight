using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using VRage.Game.ObjectBuilders;
using VRage.Game.ModAPI;
using System.Drawing.Design;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game;
using Sandbox.Game.World;
using VRage.Game;

namespace InfiniteStrike.SleepMod{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ServerSession : MySessionComponentBase{

        private int time = 0;
        private float sunTimeStep;
        private bool isSleeping = false;

        private Vector3D? targetVector;
        private Vector3D defaultSunVector = Vector3D.Zero;

        private MyPlanet currentPlanet;
        private Vector3D planetLocation;

        private static string playerMessage = "";

        List<IMyPlayer> connectedPlayers = new List<IMyPlayer>();

        public override void BeforeStart()
        {
            MyObjectBuilder_Checkpoint component = MyAPIGateway.Session.GetCheckpoint("null");
            MyObjectBuilder_SectorWeatherComponent weatherComponent = null;

            foreach(MyObjectBuilder_SessionComponent comp in component.SessionComponents){
                MyObjectBuilder_SectorWeatherComponent wComp = (comp as MyObjectBuilder_SectorWeatherComponent);
                if(wComp != null) weatherComponent = wComp;
            }

            this.defaultSunVector = new Vector3(
                weatherComponent.BaseSunDirection.X,
                weatherComponent.BaseSunDirection.Y,
                weatherComponent.BaseSunDirection.Z
            );

            this.sunTimeStep = MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes;
        }

        public override void UpdateBeforeSimulation()
        {
            if(MyAPIGateway.Session.IsServer){
                if(this.time < 60){
                    time++;
                }else{
                    if(targetVector.HasValue){
                        int currentHour = Common.ModMath.getLocalPlanetaryTime(targetVector.Value, currentPlanet, planetLocation, defaultSunVector).Hour;
                        if(currentHour < 7 || currentHour > 17){
                            ServerSession.playerMessage = "Skipping through the night....";
                            this.isSleeping = true;
                        }else{
                            ServerSession.playerMessage = "Good Morning!";
                            this.isSleeping = false;
                            this.targetVector = null;

                            connectedPlayers.Clear();
                            MyAPIGateway.Players.GetPlayers(connectedPlayers); // populate the list again;

                            foreach(IMyPlayer currentPlayer in connectedPlayers){
                                if(Common.Utils.isPlayerValid(currentPlayer)){ 

                                    // Reset the weather, kindof like that one block game ;)
                                    MyAPIGateway.Session.WeatherEffects.RemoveWeather(currentPlayer.Character.GetPosition());
                                }
                            }
                        }
                    }else{
                        this.doSleep();
                    }

                    BedLogicComponent.playerMessage = ServerSession.playerMessage;

                    if(Common.isServer()){
                        MyAPIGateway.Multiplayer.SendMessageToOthers(Common.ModInfo.pMessageHandle, MyAPIGateway.Utilities.SerializeToBinary<string>(ServerSession.playerMessage));
                    }

                    this.time = 0;

                    if(this.isSleeping){
                        this.fastForward();
                    }
                }
            }
            //base.UpdateBeforeSimulation();
        }

        public void fastForward(){
            MyAPIGateway.Session.GameDateTime = MyAPIGateway.Session.GameDateTime.AddMinutes(sunTimeStep / 1200);
        }

        public void doSleep(){
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, Common.Utils.isPlayerValid);
            int numberOccupiedBeds = Common.BedChatDispacher.GetOccupiedBedCount();

            if(players.Count == 0) return;
            if(players.Count != numberOccupiedBeds){
                ServerSession.playerMessage =  String.Format("Can't Skip the Night\n {0} players not in bed.", (players.Count - numberOccupiedBeds).ToString());
            }
            if(players.Count > 1){
                if(!Common.ModMath.CheckPlayerDistanceRequirement(ref players)){
                    ServerSession.playerMessage = String.Format("Can't Skip the Night\n players must be within {0}km of each other.", Common.ModInfo.MAX_PLAYER_DISTANCE_KM);
                    this.targetVector = null;
                }
            }

            if(Common.ModMath.isOnPlanet(players[0].GetPosition(), out currentPlanet,out planetLocation) == false){
                ServerSession.playerMessage = "Can't Skip the Night\n Its always night here...";
                this.targetVector = null;
            }else{

                int hour = Common.ModMath.getLocalPlanetaryTime(players[0].GetPosition(), currentPlanet, planetLocation, defaultSunVector).Hour;

                if ((hour < 7 || hour > 17) == false)
                {
                    ServerSession.playerMessage = "Can't Skip the Night\n Its daytime.";
                    this.targetVector = null;
                    return;
                }

                this.targetVector = players[0].GetPosition();
            }
        }
    }
}