using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Network.GameMessages.Messages;
using ACE.Managers;

namespace ACE.Network.GameAction.Actions
{
    public class GameActionRecruitFellowship
    {
        [GameAction(GameActionType.RecruitFellowship)]
        public static void Handle(ClientMessage message, Session session)
        {
            uint targetGuid = message.Payload.ReadUInt32();

            List<Session> sessions = WorldManager.GetAll(true);

            Session targetSession = sessions.Find(playerSession => playerSession.Player.Guid.Full == targetGuid);

            FellowshipManager.RecruitFellowshipMember(session.Player, targetSession.Player);
        }
    }
}
