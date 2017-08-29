using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Entity;
using ACE.Network;
using ACE.Entity.Actions;
using ACE.Network.GameMessages.Messages;

namespace ACE.Managers
{
    public class FellowshipManager
    {
        private static Dictionary<Guid, Fellowship> guidToFellowship = new Dictionary<Guid, Fellowship>();
        
        /// <summary>
        /// Method to update player vitals in a fellowship.
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateSelf(Player player)
        {
            uint playerGuid = player.Guid.Full;
            Fellowship fellowship = FindFellowshipForPlayer(player);
            fellowship.UpdateMember(playerGuid, new FellowInfo(player));
        }

        public static Fellowship FindFellowshipForPlayer(Player player)
        {
            guidToFellowship.TryGetValue(player.FellowshipGuid, out Fellowship fellowship);
            return fellowship;
        }

        public static bool IsPlayerInFellow(Player player)
        {
            return guidToFellowship.TryGetValue(player.FellowshipGuid, out Fellowship fellowship);
        }

        private static bool IsPlayerFellowshipLeader(Player player)
        {
            guidToFellowship.TryGetValue(player.FellowshipGuid, out Fellowship fellowship);
            return player.Guid.Full == fellowship.GetLeaderGuid();
        }

        public static void CreateFellowship(Player player, string fellowshipName)
        {
            new ActionChain(player, () =>
            {
                if (!IsPlayerInFellow(player))
                {
                    Fellowship fellowship = new Fellowship(player, fellowshipName);
                    player.Session.Network.EnqueueSend(new GameMessageFellowshipFullUpdate(player.Session));
                }
            }).EnqueueChain();
        }

        public static void QuitFellowship(Player player)
        {
            new ActionChain(player, () =>
            {
                Fellowship fellowship = FindFellowshipForPlayer(player);
                if (fellowship != null)
                {
                    Console.WriteLine($"{player.Name} quitting fellowship");
                    player.FellowshipGuid = Guid.Empty;
                    fellowship.RemoveMember(player);
                    player.Session.Network.EnqueueSend(new GameMessageFellowshipQuit(player.Session));

                    fellowship.BroadcastUpdate();
                }
            }).EnqueueChain();
        }

        public static void RecruitFellowshipMember(Player recruiter, Player recruitee)
        {
            new ActionChain(recruiter, () =>
            {
                new ActionChain(recruitee, () =>
                {
                    Console.WriteLine($"{recruiter.Name} recruiting {recruitee.Name}");
                    Fellowship fellowship = FindFellowshipForPlayer(recruiter);
                    fellowship.AddMember(recruitee);
                    fellowship.BroadcastUpdate();
                }).EnqueueChain();
            }).EnqueueChain();
        }

        public static void AssignNewLeader(Player oldLeader, Player targetPlayer)
        {
            new ActionChain(oldLeader, () =>
            {
                new ActionChain(targetPlayer, () =>
                {
                    Console.WriteLine($"Passing lead from {oldLeader.Name} to {targetPlayer.Name}");
                    Fellowship fellowship = FindFellowshipForPlayer(targetPlayer);

                    if (fellowship.GetLeaderGuid() == oldLeader.Guid.Full && IsPlayerInFellow(targetPlayer))
                    {
                        fellowship.SetLeaderGuid(targetPlayer.Guid.Full);
                        fellowship.BroadcastUpdate();
                    }
                    else
                    {
                        Console.WriteLine("Either that player wasnt the leader, or the target player wasnt in fellow");
                    }
                }).EnqueueChain();
            }).EnqueueChain();
        }

        public static void SubscribeToUpdates(Player player)
        {
            Fellowship fellowship = FindFellowshipForPlayer(player);
            fellowship.Subscribe(player);
        }

        public static void UnsubscribeFromUpdates(Player player)
        {
            Fellowship fellowship = FindFellowshipForPlayer(player);
            fellowship.Unsubscribe(player);
        }

        public static void DismissFellowshipMember(Player leader, Player targetPlayer)
        {
            new ActionChain(leader, () =>
            {
                new ActionChain(targetPlayer, () =>
                {
                    Fellowship fellowship = FindFellowshipForPlayer(leader);
                    fellowship.RemoveMember(targetPlayer);
                }).EnqueueChain();
            }).EnqueueChain();
        }

        // todo
        public static void UpdateLootSharingWithFellowshipMember()
        {
        }

        public static void ChangeFellowshipOpenness(Player player)
        {
            new ActionChain(player, () =>
            {
                Fellowship fellowship = FindFellowshipForPlayer(player);
                if (fellowship != null)
                {
                    if (fellowship.GetFellowshipOpenStatus() == FellowshipOpenStatus.Closed)
                    {
                        Console.WriteLine($"{player.Name} opened the fellow");
                        fellowship.SetFellowshipOpenStatus(FellowshipOpenStatus.Open);
                    }
                    else
                    {
                        Console.WriteLine($"{player.Name} closed the fellow");
                        fellowship.SetFellowshipOpenStatus(FellowshipOpenStatus.Closed);
                    }
                    fellowship.BroadcastUpdate();
                }
            }).EnqueueChain();
        }
        
        public enum FellowshipOpenStatus {
            Closed,
            Open
        }

        public class Fellowship
        {
            private string fellowshipName;
            private uint fellowshipLeaderGuid;
            public readonly Guid FellowshipGuid;

            private FellowshipOpenStatus fellowshipOpenStatus;

            private bool shareXP; // XP sharing: 0=no, 1=yes
            private bool evenXPSplit;
            private bool locked;

            private Dictionary<uint, FellowInfo> fellowshipMembers;

            public Fellowship(Player leader, string fellowshipName, bool shareXP = true)
            {
                this.shareXP = shareXP;
                this.fellowshipLeaderGuid = leader.Guid.Full;
                this.fellowshipName = fellowshipName;

                this.FellowshipGuid = Guid.NewGuid();

                this.fellowshipMembers = new Dictionary<uint, FellowInfo>();
                this.fellowshipMembers.Add(this.fellowshipLeaderGuid, new FellowInfo(leader));

                this.fellowshipOpenStatus = FellowshipOpenStatus.Closed;
                this.locked = false;
                this.evenXPSplit = true;

                guidToFellowship.Add(this.FellowshipGuid, this);
                leader.FellowshipGuid = this.FellowshipGuid;

                this.Subscribe(leader);
            }

            public uint GetLeaderGuid()
            {
                return this.fellowshipLeaderGuid;
            }

            public void SetLeaderGuid(uint newLeaderGuid)
            {    
                this.fellowshipLeaderGuid = newLeaderGuid;
            }

            internal void UpdateMember(uint playerGuid, FellowInfo fellowInfo)
            {
                this.fellowshipMembers[playerGuid] = fellowInfo;
            }

            public void AddMember(Player player)
            {
                this.fellowshipMembers.Add(player.Guid.Full, new FellowInfo(player));
                player.FellowshipGuid = this.FellowshipGuid;
            }

            public FellowshipOpenStatus GetFellowshipOpenStatus()
            {
                return this.fellowshipOpenStatus;
            }

            public void SetFellowshipOpenStatus(FellowshipOpenStatus status)
            {
                this.fellowshipOpenStatus = status;
            }

            public void BroadcastUpdate()
            {
                foreach (FellowInfo fellowInfo in this.fellowshipMembers.Values)
                {
                    fellowInfo.PushUpdate();
                }
            }

            public void RemoveMember(Player player)
            {
                this.Unsubscribe(player);
                this.fellowshipMembers.Remove(player.Guid.Full);
                player.FellowshipGuid = Guid.Empty;
            }

            public void Subscribe(Player player)
            {
                this.fellowshipMembers.TryGetValue(player.Guid.Full, out FellowInfo fellowInfo);
                fellowInfo.Subscribe();
            }

            public void Unsubscribe(Player player)
            {
                this.fellowshipMembers.TryGetValue(player.Guid.Full, out FellowInfo fellowInfo);
                fellowInfo.Unsubscribe();
            }

            public void Serialize(System.IO.BinaryWriter writer)
            {
                writer.Write((UInt16)this.fellowshipMembers.Count);

                // todo: fellowsCountTableSize?
                writer.Write((byte)0x10);
                writer.Write((byte)0x00);

                // --- FellowInfo ---

                foreach (FellowInfo fellow in this.fellowshipMembers.Values)
                {
                    // Write data associated with each fellowship member
                    fellow.Serialize(writer);
                }

                writer.WriteString16L(this.fellowshipName);
                writer.Write(this.fellowshipLeaderGuid);
                
                writer.Write(this.shareXP ? 1u : 0u);
                writer.Write(this.evenXPSplit ? 1u : 0u);
                writer.Write(this.fellowshipOpenStatus == FellowshipOpenStatus.Open ? 1u : 0u);
                writer.Write(this.locked ? 1u : 0u);

                // I suspect this is a list of recently disconnected fellows which can be reinvited, even in locked fellowship - Zegeger
                // todo
                writer.Write(0u);

                // Some kind of additional table structure, it is not read by the client. Appears to be related to some fellowship wide flag data, such as for Colloseum - Zegeger
                // todo
                writer.Write((uint)0x00200000);
                writer.Write((uint)0x00200000);
            }
        }

        internal class FellowInfo
        {
            private uint guid;
            private string name;
            private bool lootSharing;

            private System.Timers.Timer timer;

            private Session session;

            private uint level;
            private uint currentHealth;
            private uint currentStamina;
            private uint currentMana;

            private uint maxHealth;
            private uint maxStamina;
            private uint maxMana;

            public FellowInfo(Player player)
            {
                this.session = player.Session;

                this.guid = player.Guid.Full;
                this.name = player.Name;
                this.lootSharing = false;

                this.level = player.Level;

                this.maxHealth = player.Health.MaxValue;
                this.maxStamina = player.Stamina.MaxValue;
                this.maxMana = player.Mana.MaxValue;

                this.currentHealth = player.Health.Current;
                this.currentStamina = player.Stamina.Current;
                this.currentMana = player.Mana.Current;

                this.timer = new System.Timers.Timer();
                this.timer.Interval = 2000;

                this.timer.Elapsed += OnInterval;
            }

            public uint GetPlayerGuid()
            {
                return this.guid;
            }

            public void Subscribe()
            {
                PushUpdate();
                Console.WriteLine($"{session.Player.Name} subscribed to fellow updates");
                this.timer.Enabled = true;
            }

            public void Unsubscribe()
            {
                Console.WriteLine($"{session.Player.Name} unsubscribed from fellow updates");
                this.timer.Enabled = false;
            }

            private void OnInterval(Object source, System.Timers.ElapsedEventArgs args)
            {
                PushUpdate();
            }

            internal void PushUpdate()
            {
                Console.WriteLine($"Sending fellowship update to {this.session.Player.Name}");
                this.session.Network.EnqueueSend(new GameMessageFellowshipFullUpdate(session));
            }
            public void Serialize(System.IO.BinaryWriter writer)
            {
                writer.Write(this.guid);

                writer.Write(0u);
                writer.Write(0u);

                writer.Write(this.level);

                writer.Write(this.maxHealth);
                writer.Write(this.maxStamina);
                writer.Write(this.maxMana);

                writer.Write(this.currentHealth);
                writer.Write(this.currentStamina);
                writer.Write(this.currentMana);

                // todo: share loot with this fellow?
                writer.Write((uint)0x1);

                writer.WriteString16L(this.name);
            }
        }
    }
}
