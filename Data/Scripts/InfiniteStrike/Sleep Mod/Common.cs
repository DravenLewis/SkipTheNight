using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System;
using VRageMath;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using System.Threading;
using Sandbox.Game.Entities.Planet;
using VRage.ModAPI;

namespace InfiniteStrike.SleepMod
{

    public class Common
    {

        public readonly struct ModInfo
        {
            public const string MOD_NAME = "Skip the Night";
            public const string MOD_VERSION = "0.0.1";
            public const ushort pMessageHandle = 0xF00B; // Heh Foob 

            public const int MAX_PLAYER_DISTANCE_KM = 10;
        };


        public static bool isServer()
        {
            return MyAPIGateway.Utilities.IsDedicated;
        }

        public static void WriteLog(string input, params object[] args)
        {
            WriteLog(String.Format(input, args));
        }

        public static void WriteLog(string input)
        {
            MyLog.Default.WriteLineAndConsole(String.Format("[{0}: ] {1}", Common.ModInfo.MOD_NAME, input));
        }

        public static class ModMath
        {
            public static DateTime getLocalPlanetaryTime(Vector3D location, MyPlanet currentPlanet, Vector3D planetLocation, Vector3 baseSunDirection)
            {
                DateTime baseDate = new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime gameDateTime = MyAPIGateway.Session.GameDateTime;
                float sunRotationIntervalSeconds = MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes * 60;

                // Calculate days elapsed and time ratio UTC once
                float daysElapsedRaw = (float)(gameDateTime - baseDate).TotalSeconds / sunRotationIntervalSeconds;
                int daysElapsed = (int)Math.Floor(daysElapsedRaw);
                float timeRatioUTC = daysElapsedRaw - daysElapsed;

                DateTime utcTime = baseDate.AddDays(daysElapsed).AddMilliseconds(timeRatioUTC * 86400000);

                // Simplify local time calculation
                float playerAzimuth = 0f;
                float playerElevation = 0f;
                Vector3D relativePlayerPos = location - planetLocation;
                Vector3D relativePlayerPosNormal = relativePlayerPos / relativePlayerPos.Length();

                if (baseSunDirection != Vector3.Backward)
                {
                    Quaternion offset;
                    Quaternion.CreateFromTwoVectors(ref baseSunDirection, ref Vector3.Forward, out offset);
                    relativePlayerPosNormal = Vector3.Transform(relativePlayerPosNormal, offset);
                }

                Vector3.GetAzimuthAndElevation(relativePlayerPosNormal, out playerAzimuth, out playerElevation);
                float timeRatioLocal = playerAzimuth / (float)Math.PI * 12;

                DateTime localTime = utcTime.AddHours(-timeRatioLocal - (baseSunDirection == Vector3.Backward ? 0 : 12));

                return localTime;
            }

            public static bool isOnPlanet(Vector3D playerLocation, out MyPlanet planet, out Vector3D planetLocation){
                List<MyPlanet> allPlanets = new List<MyPlanet>();
                allPlanets.Clear();
                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);

                foreach(IMyEntity entity in entities){
                    if(entity.GetType() == typeof(MyPlanet)){
                        allPlanets.Add(entity as MyPlanet);
                    }
                }

                foreach(MyPlanet currentPlanet in allPlanets){
                    planetLocation = currentPlanet.PositionComp.GetPosition();
                    if(Vector3D.DistanceSquared(planetLocation, playerLocation) < Math.Pow(currentPlanet.AtmosphereRadius, 2)){
                        planet = currentPlanet;
                        return true;
                    }
                }

                planetLocation = Vector3D.Zero;
                planet = null;
                return false;
            }

            public static bool CheckPlayerDistanceRequirement(ref List<IMyPlayer> players)
            {
                int distance = 10000000 * ModInfo.MAX_PLAYER_DISTANCE_KM;
                for (int i = 0; i < players.Count; i++)
                {
                    for (int j = 0; j < players.Count; j++)
                    {
                        if (Vector3D.DistanceSquared(players[i].GetPosition(), players[j].GetPosition()) > distance)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public static class Utils
        {
            public static bool isPlayerValid(IMyPlayer player)
            {
                return player.Character != null && player.Character.GetPosition() != null && !player.Character.IsBot;
            }

            public static bool isCurrentBedValid(IMyCryoChamber bed)
            {
                bool singleplayerCheck = bed != null && bed.Pilot != null && bed.Pilot.IsPlayer && !bed.Pilot.IsBot;
                bool multiplayerCheck = MyAPIGateway.Session.LocalHumanPlayer != null && MyAPIGateway.Session.LocalHumanPlayer.Character != null && bed.Pilot.EntityId == MyAPIGateway.Session.LocalHumanPlayer.Character.EntityId;
                return (singleplayerCheck && multiplayerCheck);
            }
        }

        public static class BedChatDispacher
        {

            public static List<IMyCryoChamber> beds = new List<IMyCryoChamber>();

            public static int GetOccupiedBedCount()
            {
                List<IMyPlayer> sleepingPlayers = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(sleepingPlayers, Common.Utils.isPlayerValid);

                int numberPlayers = 0;

                foreach (IMyCryoChamber bed in beds)
                {
                    if (bed.Pilot == null) continue;
                    if (!bed.Pilot.IsPlayer) continue;

                    if (sleepingPlayers.FindIndex((x) =>
                    {
                        return x.Character.EntityId == bed.Pilot.EntityId;
                    }) > -1)
                    {
                        numberPlayers++;
                    };
                }

                return numberPlayers;
            }

        }
    }
}