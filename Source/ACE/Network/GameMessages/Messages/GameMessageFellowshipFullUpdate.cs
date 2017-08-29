using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Common.Extensions;
using ACE.Entity.Actions;
using ACE.Entity;
using ACE.Managers;

namespace ACE.Network.GameMessages.Messages
{
    public class GameMessageFellowshipFullUpdate : GameMessage
    {
        public GameMessageFellowshipFullUpdate(Session session)
            : base(GameMessageOpcode.GameEvent, GameMessageGroup.Group09)
        {
            Writer.Write(session.Player.Guid.Full);
            Writer.Write(session.GameEventSequence++);
            Writer.Write((uint)GameEvent.GameEventType.FellowshipFullUpdate);
          
            FellowshipManager.FindFellowshipForPlayer(session.Player).Serialize(Writer);
        }
    }
}
