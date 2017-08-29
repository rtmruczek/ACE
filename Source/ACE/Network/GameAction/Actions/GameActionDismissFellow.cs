using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Managers;

namespace ACE.Network.GameAction.Actions
{
    public static class GameActionDismissFellow
    {
        [GameAction(GameActionType.CreateFellowship)]
        public static void Handle(ClientMessage message, Session session)
        {
            var targetGuid = message.Payload.ReadUInt16();
            List<Session> sessions = WorldManager.GetAll(true);

            Session targetSession = sessions.Find(playerSession => playerSession.Player.Guid.Full == targetGuid);

            FellowshipManager.DismissFellowshipMember(session.Player, targetSession.Player);
        }
    }
}
