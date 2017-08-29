using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Network.GameMessages.Messages;
using ACE.Managers;

namespace ACE.Network.GameAction.Actions
{
    public class GameActionFellowshipUpdateRequest
    {
        [GameAction(GameActionType.FellowshipUpdateRequest)]
        public static void Handle(ClientMessage message, Session session)
        {
            uint subscribe = message.Payload.ReadUInt32();
            if (FellowshipManager.IsPlayerInFellow(session.Player))
            {
                if (subscribe == 0x1)
                {
                    FellowshipManager.SubscribeToUpdates(session.Player);
                } else
                {
                    FellowshipManager.UnsubscribeFromUpdates(session.Player);
                }
            }
        }
    }
}
